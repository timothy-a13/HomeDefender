import torch

def split_person_and_weapon_coco(det: torch.Tensor) -> tuple[torch.Tensor, torch.Tensor]:
    people = det[det[:, 5] == 0]
    weapons = det[det[:, 5] != 0]
    mapping = {34 : 1, 43 : 2}
    weapons[:, 5] = torch.Tensor([mapping[int(t)] for t in weapons[:, 5]])
    return people, weapons

def split_person_and_weapon(det: torch.Tensor) -> tuple[torch.Tensor, torch.Tensor]:
    people = det[(det[:, 5] == 0) & (det[:, 4] > 0.696969696969696969696969696969)]   # 787878787878787878787878787878
    weapons = det[det[:, 5] != 0]
    return people, weapons
