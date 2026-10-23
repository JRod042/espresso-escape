using UnityEngine;

public static class ShotYard
{
    public static string Main()
    {
        var cam = Camera.main;
        var follow = cam.GetComponent<SetCameraFollow>();
        var bean = GameObject.Find("Imagine Bean").transform;
        var pos = new Vector3(bean.position.x + follow.offset.x, follow.restY + follow.offset.y, follow.offset.z);
        cam.transform.SetPositionAndRotation(pos, Quaternion.Euler(follow.pitch, 0f, 0f));
        cam.enabled = true;
        return cam.transform.position + " " + cam.transform.eulerAngles;
    }
}

public static class ShotGap
{
    // Low play-camera angle on the kraft arch: the opening under the deck and its underside.
    // Scene has no play-mode hop blocks (they are built only in Play). The arch is the platform in the scene.
    public static string Main()
    {
        var cam = Camera.main;
        var bridge = GameObject.Find("Kraft Bridge").transform;
        // Look through the arch so the woven mat and the corrugated underside are both in frame.
        var target = bridge.position + new Vector3(0.2f, 0.16f, 0f);
        var pos = bridge.position + new Vector3(-1.45f, 0.42f, -1.65f);
        cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(target - pos, Vector3.up));
        cam.enabled = true;
        return cam.transform.position + " -> " + target;
    }
}

public static class ShotWall
{
    public static string Main()
    {
        var cam = Camera.main;
        cam.transform.SetPositionAndRotation(new Vector3(0.15f, 2.65f, -0.35f), Quaternion.Euler(4f, 0f, 0f));
        cam.enabled = true;
        return cam.transform.position + " " + cam.transform.eulerAngles;
    }
}
