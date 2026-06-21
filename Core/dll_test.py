from pythonnet import load
load('coreclr')

import clr
clr.AddReference('dll/Pipe')

from Pipe import PipeStreams
PipeStreams(int(input())).SendAll("222 223 255")