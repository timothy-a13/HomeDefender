import numpy as np
import Risk
import Weapon

class ChangedState:
    def __init__(self) -> None:
        self.person_record = {}
        '''
        Structure:
        {id: {info: ndarray([bbox, id, cls, state, weapon]), age: int}}
        '''

    def update(self, risk_dets, weapon_dets):
        result = []

        for det in risk_dets:
            id = int(det[4])
            state = int(det[6])

            if id in self.person_record.keys(): 
                if state > self.person_record[id]['info'][6]:
                    self.person_record[id]['info'][6] = state
                    result.append(det)

            else:
                result.append({id: {'info': np.concatenate((det[:], [Weapon.WeaponState.Empty])), 'age': 0}})

        for det in weapon_dets:
            pass