import math
import time
import enum
import numpy as np
from datetime import datetime
from trackers.strongsort.utils.parser import get_config
from types import DynamicClassAttribute



def create_weaponer(tracker_config):
    cfg = get_config()
    cfg.merge_from_file(tracker_config)
    return WeaponTracker(cfg.ocsort.max_age)


class WeaponTracker:
    def __init__(self, max_age=50) -> None:
        self.max_age = max_age
        self.people:list[Person] = []   # Used to store all undeleted people

    def update(self, dets, weapons):
        # Parse array (dets) into dictionary (dets_dict)
        dets_dict = {} if dets is None else {int(det[4]): det for det in dets}
        weapons = np.zeros((0, 6)) if weapons is None else weapons
        is_exit = []   # Used to store all people still present on the screen
        outputs = []   # Used to store all abnormal state of people
        #print(dets_dict)

        for id in set(dets_dict.keys()) - set([p.id for p in self.people]):
            self.people.append(Person(dets_dict[id]))

        # Iterate through all the people still stored
        for person in self.people:
            if person.id in dets_dict.keys():   # If the traversed person exits in the current frame.
                is_exit.append(person.id)
                if (state := person.update(dets_dict[person.id], weapons)) is not None:
                    outputs.append(state)
            else:
                person.update(None, weapons)

        self.people = [p for p in self.people if p.e_age < self.max_age]

        return np.array(outputs) if len(outputs) else None


class WeaponState(enum.IntEnum):
    Empty = 0
    BAT   = 1
    KNIFE = 2
    GUN   = 3

class Person:
    threshold = 30

    def __init__(self, det:np.ndarray) -> None:
        self.id, self.cls = map(int, det[4:6])
        self.bbox = Person.padding_bbox(det[:4])
        self.e_age = 0
        self.count = 0
        self.counter = np.zeros(4, 'int32')
        self.last_counter = np.zeros(4, 'int32')
        self.confs = np.zeros(4, 'float')

    def update(self, det, weapons):
        state = None

        if det is None:
            self.e_age += 1
        else:
            self.e_age = 0
            self.bbox = Person.padding_bbox(det[:4])
            has_weapon = WeaponState.Empty
            for weapon in weapons:
                w_cls = int(weapon[5])
                w_conf = weapon[4]
                w_bbox = weapon[:4]
                if Person.is_overlap(self.bbox, w_bbox):
                    has_weapon = WeaponState(w_cls) if w_cls > has_weapon.value else has_weapon
                    self.counter[has_weapon] += 1
                    c = self.counter[has_weapon]
                    self.confs[has_weapon] = self.confs[has_weapon] * (c - 1) / c + w_conf / c
                
            self.count += bool(has_weapon)

            if self.count >= self.threshold and Person.should_change(self.last_counter, self.counter):
                #if has_weapon > self.weapon_state:
                confidence = np.zeros(4, 'float')
                total = self.counter.sum()
                for i in range(1, 4):
                    confidence[i] = self.confs[i] * self.counter[i] / total if self.counter[i] else -1
                state = np.concatenate((det[0:6], confidence[1:]))
                self.last_counter[:] = self.counter

        return state

    @DynamicClassAttribute
    def has_weapon(self):
        return self.count >= Person.threshold

    @staticmethod
    def padding_bbox(bbox:np.ndarray, rate=.1) -> np.ndarray:
        width = bbox[2] - bbox[0]
        width *= rate
        return np.array([bbox[0] - width, bbox[1], bbox[2] + width, bbox[3]])
    
    @staticmethod
    def is_overlap(bbox1:np.ndarray, bbox2:np.ndarray) -> bool:
        return not (
            bbox1[2] < bbox2[0] or bbox2[2] < bbox1[0] or
            bbox1[3] < bbox2[1] or bbox2[3] < bbox1[1]
        )
    
    @staticmethod
    def should_change(last: np.ndarray, cur: np.ndarray):
        return not last.any() or last.argmax() != cur.argmax() and Person.check_over70(cur)
        
    @staticmethod
    def check_over70(count: np.ndarray):
        return count.max() / count.sum() > 0.7
