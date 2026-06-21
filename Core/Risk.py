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
        self.id  = int(det[4])
        self.cls = int(det[5])
        self.avg_x = Person._get_x(det[[0, 2]])
        self.avg_y = Person._get_y(det[[1, 3]])
        self.cur_rate     = 0.1
        self.vertex_count = 0
        self.diffs = []
        self.orbit = []
        self.e_age = 0   # Empty age
        self.has_vertex = 0
        self.pre_time = time.time()
        self.state = State.PASS
        self.bgn = datetime.now()
        # tmp
        self.y = []

        self.weapon_state = Weapon.WeaponState.Empty

    def update(self, det, weapon_det, fps=24):
        fpsx2 = fps * 2
        state_change = None
        thredhold = 250

        if det is None:
            self.e_age += 1
        else:
            self.e_age = 0
            # 計算bbox的底部中心點
            cur_x = Person._get_x(det[[0, 2]])
            cur_y = Person._get_y(det[[1, 3]])
            # 計算與上一步之間的位移量
            dx = abs(self.avg_x - cur_x)
            dy = abs(self.avg_y - cur_y)

            # 與上一步的時間差
            ms = time.time() - self.pre_time
            self.diffs.append(math.hypot(dx, dy) / ms)
            while len(self.diffs) > fpsx2: self.diffs.pop(0)
            self.orbit.append(m := np.mean(self.diffs))
            self.pre_time = time.time()

            # tmp
            self.y.append(m)

            if len(self.orbit) > fpsx2 and                                                            \
              (self.orbit[-fpsx2] - self.orbit[-fps] > 0) ^ (self.orbit[-fps] - self.orbit[-1] > 0)   \
              and not self.has_vertex and self.orbit[-fps] >= thredhold:
                self.vertex_count += 1
                self.has_vertex += fpsx2
            elif self.has_vertex > 0:
                self.has_vertex -= 1

            if self.vertex_count == 3 and self.state < 2:
                self.state = State.WANDER
                state_change = np.concatenate((det[0:6], [int(State.WANDER), self.weapon_state]))

            if len(self.orbit) > fps and self.orbit[-fps] < thredhold and self.state < 1:
                self.vertex_count = 0
                self.state = State.WAIT
                state_change = np.concatenate((det[0:6], [int(State.WAIT), self.weapon_state]))

            self.avg_x = (1 - self.cur_rate) * self.avg_x + self.cur_rate * cur_x
            self.avg_y = (1 - self.cur_rate) * self.avg_y + self.cur_rate * cur_y

            if weapon_det is not None:
                self.weapon_state = Weapon.WeaponState(weapon_det[6])
                state_change = np.concatenate((det[0:6], [self.state, weapon_det[6]]))

        return state_change

    @staticmethod
    def _get_x(d):
        return (d[0] + d[1]) / 2
    
    @staticmethod
    def _get_y(d):
        return d[1] #d[0] + (d[1] - d[0]) * 0.9
    
    def __del__(self):
        
        import matplotlib.pyplot as plt
        plt.plot(range(1, len(self.y) + 1, 1), self.y, label=str(self.id))
        plt.title(str(self.id))
        plt.show()

        '''
        print(self.state, self.state > 0)
        if self.state > 0:
            print("Store...")
            StoreDangerousFragmentThread(self.bgn, datetime.now()).start()
        '''