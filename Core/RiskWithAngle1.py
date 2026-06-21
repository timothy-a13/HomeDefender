import math
import time
import enum
import numpy as np
from datetime import datetime
from trackers.strongsort.utils.parser import get_config
from StoreDangerousFragmentThread import StoreDangerousFragmentThread
import Weapon



def create_risker(tracker_config):
    cfg = get_config()
    cfg.merge_from_file(tracker_config)
    return RiskClassifier(cfg.ocsort.max_age)


test_index = 0
class RiskClassifier:
    def __init__(self, max_age=50) -> None:
        self.max_age = max_age
        self.people:list[Person] = []   # Used to store all undeleted people

    def update(self, dets, weapon_dets, fps=24):
        # Parse array (dets) into dictionary (dets_dict)
        dets_dict = {} if dets is None or not len(dets) else {int(det[4]): det for det in dets}
        weapon_dets_dict = {} if weapon_dets is None else {int(det[4]): det for det in weapon_dets}
        is_exit = []   # Used to store all people still present on the screen
        outputs = []   # Used to store all abnormal state of people
        #print(dets_dict)

        # Iterate through all the people still stored
        for person in self.people:
            if person.id in dets_dict.keys():   # If the traversed person exits in the current frame.
                is_exit.append(person.id)
                if (state := person.update(dets_dict[person.id], weapon_dets_dict.get(person.id), fps)) is not None:
                    outputs.append(state)
            else:
                #print(person.id, person.e_age)
                person.update(None, fps)

        for id in set(dets_dict.keys()) - set([p.id for p in self.people]):
            self.people.append(Person(dets_dict[id], fps))

        self.people = [p for p in self.people if p.e_age < self.max_age]

        return np.array(outputs) if len(outputs) else None



class State(enum.IntEnum):
    PASS   = 0
    WAIT   = 1
    WANDER = 2

class Person:
    def __init__(self, det:np.ndarray, fps) -> None:
        # info
        self.id  = int(det[4])
        self.cls = int(det[5])
        # angle
        self.pre_x = Person._get_x(det[[0, 2]])
        self.pre_y = Person._get_y(det[[1, 3]])
        self.pre_angle = None
        self.total_angle = 0   # 每次角度差的總和（積分）
        self.t_angle_list = [] # 儲存角度差的陣列
        self.e_age = 0   # Empty age
        self.a_age = 1   # Appear age（總共出現的幀數）
        self.t_age = 1   # Total age（從人物第一次出現開始計算的幀數）
        self.lastp = 0   # Last record point（最後一個被計算的幀）
        self.min = None
        self.max = None
        # speed
        self.p_x = Person._get_x(det[[0, 2]])
        self.p_y = Person._get_y(det[[1, 3]])
        self.speeds = []
        self.record = []
        self.size = det[2] - det[0]
        # time
        self.bgn = datetime.now()
        # state
        self.state = State.PASS
        self.weapon_confs = np.array([-1] * 3, 'float')
        # tmp
        self.ya = []
        self.ys = []
        self.th = []

    def update(self, det, weapon_det, fps=24):
        BGN_FRAME = 16
        state_change = None
        rate = 0.15

        if det is None:
            self.e_age += 1
        else:
            self.e_age = 0
            
            # 計算bbox的中心點
            cur_x = Person._get_x(det[[0, 2]])
            cur_y = Person._get_y(det[[1, 3]])
            self.size = self.size * (1 - rate) + (det[2] - det[0]) * rate
            thredhold = self.size / 115 + 12 / 23 #(self.size + 175) / 230
            #thredhold = 7 / (2 * 630 ** 2 - 397050) * self.size ** 2 + (1 / 230 - 1190 / (630 ** 2 - 198525)) * self.size + (35 / 46 - 21175 / (2 * 630 ** 2 -397050) + 65450 / (630 ** 2 - 198525))
            self.th.append(thredhold)

            # 速度計算區
            dx = abs(self.p_x - cur_x)
            dy = abs(self.p_y - cur_y)
            self.speeds.append(math.hypot(dx, dy))
            while len(self.speeds) > fps * 2 / 3: self.speeds.pop(0)
            self.record.append(m := np.mean(self.speeds))
            if len(self.record) > 3: self.record.pop(0)
            record = np.array(self.record)
            self.p_x, self.p_y = cur_x, cur_y
            self.ys.append(m)
            
            # 角度計算區（每8幀一次）
            if self.t_age >= BGN_FRAME and self.t_age - self.lastp >= fps // 3:
                # 計算前一點和這一點連線的角度
                hypot = math.hypot(abs(cur_x - self.pre_x), abs(cur_y - self.pre_y)) + 1e-30
                angle = math.acos((cur_x - self.pre_x) / hypot) * (-1 if cur_y - self.pre_y < 0 else 1) / math.pi * 180 # if hypot else 0

                if self.pre_angle is not None:
                    # 計算前一個角度和這一個角度的差
                    d = Person._angle_diff(angle, self.pre_angle)
                    self.total_angle += d
                    self.t_angle_list.append(self.total_angle)
                    if len(self.t_angle_list) >= 2:
                        t_angle = self.t_angle_list[-2]
                        self.t_angle_list.pop(0)
                        if self.min is None or self.min > t_angle:
                            self.min = t_angle
                        elif self.max is None or self.max < t_angle:
                            self.max = t_angle
                    self.ya.append(self.total_angle)

                self.pre_angle = angle
                self.pre_x, self.pre_y = cur_x, cur_y
                self.lastp = self.t_age

                if np.all(record < thredhold) and self.state == 1:
                    #print(f'Is wait: {self.state == 1}')
                    self.min = self.max = None
                    self.total_angle = 0

                if self.min is not None and self.max is not None:
                    if np.all(record < thredhold) and self.max - self.min > 120 and self.state < 1:
                        self.state = State.WAIT
                        state_change = np.concatenate((det[0:6], [int(State.WAIT)], self.weapon_confs))
                    elif not np.all(record < thredhold) and self.max - self.min > 150 and self.state < 2:
                        self.state = State.WANDER
                        state_change = np.concatenate((det[0:6], [int(State.WANDER)], self.weapon_confs))

            if weapon_det is not None:
                self.weapon_confs = weapon_det[6:]
                state_change = np.concatenate((det[0:6], [self.state], weapon_det[6:]))

            self.a_age += 1
        self.t_age += 1

        return state_change

    @staticmethod
    def _get_x(d):
        return (d[0] + d[1]) / 2
    
    @staticmethod
    def _get_y(d):
        return (d[0] + d[1]) / 2
    
    @staticmethod
    def _angle_diff(cur: float, pre: float):
        d = cur - pre
        if 180 < d:
            d = (360 - d)
        elif -180 > d:
            d = (-360 - d)
        return d
    
    def __del__(self):
        if len(self.ya) > 5:
            print(f'{self.id}: {np.mean(self.th)}')
        '''
            import matplotlib.pyplot as plt
            plt.plot(range(1, len(self.ya) + 1, 1), self.ya, label=str(self.id))
            plt.title(str(self.id) + ' angle')
            plt.show()
            plt.plot(range(1, len(self.ys) + 1, 1), self.ys, label=str(self.id))
            plt.title(str(self.id) + ' speed')
            plt.show()
        '''
        print(self.state, self.state > 0)
        if self.state > 0:
            print("Store...")
            StoreDangerousFragmentThread(self.bgn, datetime.now()).start()