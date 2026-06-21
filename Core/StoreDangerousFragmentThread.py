from threading import Thread
from datetime import datetime
from time import sleep
from shutil import copy2, move
from os import listdir, path, mkdir, renames   #, remove
from bisect import bisect_left, bisect_right
from pathlib import Path
from NatificationSendThread import dangerous_video_notification
import ffmpeg
from home_defender_config import VIDEO_ROOT, connect_sql



TS          = '.ts'
DANGEROUS   = 'dangerous\\'
TIME_FORMAT = '%Y-%m-%d-%H-%M-%S'

cam_id: int
g_key: str

class StoreDangerousFragmentThread(Thread):
    def __init__(self, bgn : datetime, end : datetime) -> None:
        super().__init__()
        self.root = VIDEO_ROOT
        self.folder = str(self.root / g_key) + path.sep
        self.bgn = bgn
        self.end = end
        print(g_key)

    def run(self) -> None:
        print('pass 1')
        try:
            while True:
                for file in listdir(self.folder)[::-1]:
                    if file[-3:] == '.ts' and datetime.strptime(file, TIME_FORMAT + TS) > self.end:
                        raise Exception('Exit loop.')
                sleep(0.001)
        except Exception as e:
            print(e)

        print('pass 2')
        bgn_file = self.bgn.strftime(TIME_FORMAT)   # No suffix
        end_file = self.end.strftime(TIME_FORMAT)   # No suffix
        files = listdir(self.folder)
        files.sort()
        files.pop()   # pop .m3u8
        files.pop()   # pop dangerous folder

        print('pass 3')
        bgn_ind = bisect_left (files, bgn_file + TS) - 3   #bgn_ind = files.index(bgn_file + TS)
        end_ind = bisect_right(files, end_file + TS)       #end_ind = files.index(bgn_file + TS) + 1
        bgn_ind = 0 if bgn_ind < 0 else bgn_ind
        if not self._is_playable(self.folder + files[bgn_ind]):
            bgn_ind = bgn_ind - 1 if bgn_ind > 0 else bgn_ind + 1
        bgn_file = files[bgn_ind][:-3]
        end_file = files[end_ind-1][:-3]

        print('pass 4')
        # Get all the folders in 'dangerous' folder
        fragments = [f for f in listdir(self.folder + DANGEROUS) if path.isdir(self.folder + DANGEROUS + f)]
        is_covered = False

        print('pass 5')
        for frag in fragments:
            frag_bgn, frag_end = frag.split('~')   # No suffix
            if is_covered := self._covered(bgn_file, end_file, frag_bgn, frag_end):
                break

        print('pass 6')
        if is_covered:
            for f in files[bgn_ind:end_ind]:
                copy2(self.folder + f, self.folder + DANGEROUS + frag + '\\' + f)
            bgn_file = min(bgn_file, frag_bgn)
            end_file = max(end_file, frag_end)
            renames(self.folder + DANGEROUS + frag, self.folder + DANGEROUS + bgn_file + '~' + end_file)
        else:
            new_folder_name = bgn_file + '~' + end_file + '\\'
            mkdir(self.folder + DANGEROUS + new_folder_name)
            for f in files[bgn_ind:end_ind]:
                copy2(self.folder + f, self.folder + DANGEROUS + new_folder_name + f)

        print('pass 9')
        self._write_m3u8(self.folder, bgn_file, end_file)

        print('pass 11')
        self._write_sql(cam_id, bgn_file, end_file)

        print('pass 10')
        task = dangerous_video_notification(cam_id, bgn_file + '~' + end_file)
        task.start()

        print('pass 12')
        task.join()
        del task

        print('pass 13')


    @staticmethod
    def _covered(bgn1, end1, bgn2, end2) -> bool:
        return bgn1 <= bgn2 <= end1 or bgn1 <= end2 <= end1   \
            or bgn2 <= bgn1 <= end2 or bgn2 <= bgn1 <= end2

    @staticmethod
    def _write_m3u8(folder, bgn : str, end : str) -> None:   # folder must be an absolute path
        folder = Path(folder)                                # ex. E:\\SeniorProject\\live\\g_key
        index_file = folder / 'index.m3u8'
        with open(index_file, 'r', encoding='utf-8') as file:
            lines = list(map(lambda e: e.strip(), file.readlines()))
        
        m3u8_head = [
            '#EXTM3U\n',
            '#EXT-X-VERSION:3\n',
            '#EXT-X-PLAYLIST-TYPE:VOD\n',
            '#EXT-X-TARGETDURATION:3\n',
            '#EXT-X-MEDIA-SEQUENCE:0\n',
        ]

        fragment_folder = folder / DANGEROUS / (bgn + '~' + end)
        with open(fragment_folder / 'index.tmp.m3u8', 'w', encoding='utf-8') as file:
            file.writelines(m3u8_head)

            for i in range(5, len(lines)):   # skip the header of the original file (4)
                if lines[i][-3:] == '.ts' and bgn <= lines[i][:-3] <= end:
                    file.write(lines[i - 1] + '\n')
                    file.write(lines[i] + '\n')

            file.write('#EXT-X-ENDLIST\n')

        move(fragment_folder / 'index.tmp.m3u8', fragment_folder / 'index.m3u8')

    @staticmethod
    def _write_sql(cam_id, bgn : str, end : str):
        sql = connect_sql()

        sql.execute_query(f"SELECT time FROM cam_danger WHERE cam_id = {cam_id}")
        for row in sql:
            b, e = row['time'].split('~')
            if StoreDangerousFragmentThread._covered(b, e, bgn, end):
                is_covered_fregment = row['time']
                sql.execute_query(f"DELETE FROM cam_danger WHERE cam_id = {cam_id} and time = '{is_covered_fregment}'")
                break

        sql.execute_query(f"INSERT INTO cam_danger VALUES({cam_id}, '{bgn + '~' + end}')")
        sql.close()

    @staticmethod
    def _is_playable(file_path) -> bool:
        try:
            probe = ffmpeg.probe(file_path, v='error')
            video_stream = next((stream for stream in probe['streams'] if stream['codec_type'] == 'video'), None)
            return video_stream['level'] != -99
        except ffmpeg.Error as e:
            print(e.stderr)
            return False


    def __del__(self):
        print('Finish Store Dangerous Fragment')



        '''
        folder = Path(folder)

        files = listdir(folder)
        files.sort()
        files.pop()   # pop .m3u8

        with open(folder / 'index.tmp.m3u8', 'w', encoding='utf-8') as file:
            m3u8_head = [
                '#EXTM3U',
                '#EXT-X-VERSION:3',
                '#EXT-X-TARGETDURATION:2',
                '#EXT-X-MEDIA-SEQUENCE:0',
                '#EXT-X-PLAYLIST-TYPE:VOD',
                '#EXT-X-DISCONTINUITY'
            ]
            file.writelines()
        '''
