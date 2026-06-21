from threading import Thread
import json
from home_defender_config import connect_sql

from pythonnet import load
load('coreclr')
import clr
clr.AddReference('dll/Pipe')

from Pipe import PipeStreams
from System import Array, String

class NatificationSendThread(Thread):
    def __init__(self, cam_id, messages) -> None:
        super().__init__()
        self.cam_id = cam_id
        self.messages = messages

    def run(self) -> None:
        ipcs = self.__get_ipcs_from_sql()
        ipcs = Array[String](ipcs)
        pipes = PipeStreams(ipcs)
        for msg in self.messages:
            failed_list = pipes.SendAll(msg)
            failed_list = list(map(str, failed_list))
            self.__delete_ipcs_from_sql(failed_list)
        del pipes

    def __get_ipcs_from_sql(self) -> list:
        sql = connect_sql()

        sql.execute_query(f'SELECT ipc_id FROM IPC_table WHERE cam_id = {self.cam_id}')
        rows = [row['ipc_id'] for row in sql]
        sql.close()

        return rows
    
    def __delete_ipcs_from_sql(self, failed_list) -> None:
        sql = connect_sql()

        for failed in failed_list:
            sql.execute_query(f"DELETE FROM IPC_table WHERE ipc_id = '{failed}'")
        sql.close()

    
def dangerous_video_notification(cam_id, folder_name) -> NatificationSendThread:
    msg = "sto; None; "
    msg += json.dumps({
        'cam_id': cam_id,
        'time': folder_name,
    })

    return NatificationSendThread(cam_id, [msg])

def real_time_notification(cam_id, dets):
    messages = []
    for det in dets:
        msg = "dan; None; "
        score_map = [0, 3, 5]
        w = det[7:10].argmax()
        w = 0 if det[7:10][w] < 0 else w + 1
        w_score_map = [0, 3, 4, 5]
        msg += json.dumps({
            'cam_id': cam_id,
            'id': int(det[4]),
            'state': int(det[6]),
            'bat': det[7],
            'knife': det[8],
            'gun': det[9],
            'score': score_map[int(det[6])] + w_score_map[w]
        })
        messages.append(msg)

    return NatificationSendThread(cam_id, messages)
