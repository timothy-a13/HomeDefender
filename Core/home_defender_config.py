import os
from pathlib import Path

import pymssql._mssql as mssql


def _required(name: str) -> str:
    value = os.getenv(name)
    if not value:
        raise RuntimeError(f"Required environment variable is not set: {name}")
    return value


def connect_sql():
    return mssql.connect(
        server=os.getenv("HOMEDEFENDER_DB_SERVER", "localhost"),
        user=_required("HOMEDEFENDER_DB_USER"),
        password=_required("HOMEDEFENDER_DB_PASSWORD"),
        database=os.getenv("HOMEDEFENDER_DB_NAME", "detection_sys_db"),
    )


VIDEO_ROOT = Path(
    os.getenv("HOMEDEFENDER_VIDEO_ROOT", Path.cwd() / "live")
).expanduser().resolve()

