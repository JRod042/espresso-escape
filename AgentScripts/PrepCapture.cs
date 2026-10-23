using UnityEngine;

public static class PrepCapture
{
    public static string Main()
    {
        var session = GameSession.I;
        if (session != null) session.PlayLevel(1);
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++) canvases[i].enabled = false;
        var cam = Camera.main;
        var follow = cam != null ? cam.GetComponent<SetCameraFollow>() : null;
        if (follow != null) follow.enabled = true;
        var beanGo = GameObject.Find("Imagine Bean");
        if (cam == null || follow == null || beanGo == null || session == null)
            return "MISSING cam=" + (cam != null) + " follow=" + (follow != null) + " bean=" + (beanGo != null) + " session=" + (session != null);
        var bean = beanGo.transform;
        var pos = new Vector3(bean.position.x + follow.offset.x, follow.restY + follow.offset.y, follow.offset.z);
        cam.transform.SetPositionAndRotation(pos, Quaternion.Euler(follow.pitch, 0f, 0f));
        return "phase=" + session.phase + " cam=" + cam.transform.position + " canvases=" + canvases.Length + " bean=" + bean.position;
    }
}
