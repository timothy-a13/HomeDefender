# Third-Party Notices

HomeDefender's original code is distributed under GPL-3.0-only. Third-party
code, assets, model files, frameworks, and external services remain subject
to their own licenses and copyright notices. A third-party license is not
replaced or restricted by HomeDefender's project license.

## Bundled AI and Tracking Code

### Yolov8 Tracking / BoxMOT predecessor snapshot

- Project: <https://github.com/mikel-brostrom/yolov8_tracking>
- Repository location: `Core/`, especially `Core/trackers/`
- Snapshot metadata: version 8.0, released in February 2023
- License in this distributed snapshot: GPL-3.0
- Preserved notices: `Core/LICENSE` and `Core/CITATION.cff`

The upstream project has changed over time. The license recorded here is the
GPL-3.0 license shipped with the historical snapshot actually included in
HomeDefender, rather than the license of a newer upstream revision.

### Ultralytics YOLOv8

- Project: <https://github.com/ultralytics/ultralytics>
- Bundled version: 8.0.20-era source
- Repository location: `Core/yolov8/`
- License: GPL-3.0
- Preserved notices: `Core/yolov8/LICENSE`,
  `Core/yolov8/CITATION.cff`, and source-file headers

### OC-SORT

- Project: <https://github.com/noahcao/OC_SORT>
- Repository location: `Core/trackers/ocsort/` and related tracker adapters
- License: MIT
- Copyright: Copyright (c) 2021 Yifu Zhang
- License terms: `licenses/OC-SORT-LICENSE.txt`

### FilterPy Kalman filter implementation

- Project: <https://github.com/rlabbe/filterpy>
- Repository locations:
  `Core/trackers/ocsort/kalmanfilter.py` and
  `Core/trackers/deepocsort/kalmanfilter.py`
- License: MIT
- Copyright notice retained in source:
  Copyright 2014-2018 Roger R. Labbe Jr.
- Upstream license notice: `licenses/FilterPy-LICENSE.txt`

## Bundled Web Assets

### hls.js 1.4.7

- Project: <https://github.com/video-dev/hls.js>
- Repository files: `BlazorApp1/wwwroot/js/hls.js` and `hls.js.map`
- License: Apache-2.0
- Copyright: Copyright (c) 2017 Dailymotion
- Additional notices retained in the bundle include Brightcove,
  DASH Industry Forum, and vtt.js contributors.
- Preserved upstream notice: `licenses/hls.js-LICENSE.txt`
- License terms: `licenses/Apache-2.0.txt`

### mpegts.js 1.7.3

- Project: <https://github.com/xqq/mpegts.js>
- Repository files: `BlazorApp1/wwwroot/js/mpegts.js` and `mpegts.js.map`
- License: Apache-2.0
- Bundled dependency notices, including the ES6 Promise MIT notice, remain
  embedded in the distributed JavaScript file.
- License terms: `licenses/Apache-2.0.txt`

### Bootstrap 5.1.0

- Project: <https://github.com/twbs/bootstrap>
- Repository files: `BlazorApp1/wwwroot/css/bootstrap/`
- License: MIT
- Copyright (c) 2011-2021 Twitter, Inc.
- Copyright (c) 2011-2021 The Bootstrap Authors
- License terms: `licenses/Bootstrap-LICENSE.txt`

### Open Iconic

- Project: <https://github.com/iconic/open-iconic>
- Repository files: `BlazorApp1/wwwroot/css/open-iconic/`
- Icon code and styles: MIT, Copyright (c) 2014 Waybury
- Font files: SIL Open Font License 1.1, Copyright (c) 2014 Waybury
- Preserved license files:
  `BlazorApp1/wwwroot/css/open-iconic/ICON-LICENSE` and
  `BlazorApp1/wwwroot/css/open-iconic/FONT-LICENSE`

### Chart.js 4.4.0

- Project: <https://github.com/chartjs/Chart.js>
- Used from a public CDN by `BlazorApp1/Pages/_Layout.cshtml`
- License: MIT
- Copyright (c) 2014-2022 Chart.js Contributors
- License terms: `licenses/Chart.js-LICENSE.txt`

## External Runtime Projects

The following projects are required or supported at runtime but their source
or binaries are not redistributed as part of this repository:

- [SRS](https://github.com/ossrs/srs) — MIT
- [FFmpeg](https://ffmpeg.org/) — primarily LGPL-2.1-or-later; a build may
  become GPL-2.0-or-later or GPL-3.0-or-later depending on enabled options
- [ASP.NET Core Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
  — MIT
- [NGINX](https://nginx.org/) — 2-clause BSD

Python and NuGet dependencies are installed separately from
`Core/requirements.txt` and the `.csproj` manifests. Their package metadata
and distributions contain the authoritative license and copyright notices.

## License Compatibility Basis

GPL-3.0-only was selected for HomeDefender's original code because the
repository distributes and modifies GPL-3.0 tracking and YOLO source.
The bundled MIT, Apache-2.0, BSD-style, and LGPL-2.1-or-later components are
compatible with GPLv3 use or remain separate works in an aggregate. Assets
under the SIL Open Font License and separately executed services retain their
own licenses.
