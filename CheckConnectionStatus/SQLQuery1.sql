--SELECT * FROM camera_info;
--UPDATE camera_info SET is_conn = 1 WHERE g_key = 'example-camera-key';
SELECT g_key FROM camera_info --WHERE cam_id = 20001
