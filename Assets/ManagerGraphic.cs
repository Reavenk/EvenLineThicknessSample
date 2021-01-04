// MIT License
// 
// Copyright (c) 2021 Pixel Precision, LLC
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Collections.Generic;
using UnityEngine;

public class ManagerGraphic : UnityEngine.UI.Graphic
{

    public static Vector2 PerpCCW(Vector2 v) => new Vector2(-v.y, v.x);
    public static Vector2 PerpCW(Vector2 v) => new Vector2(v.y, -v.x);

    /// <summary>
    /// The width of the edge to create.
    /// </summary>
    float width = 10.0f;

    /// <summary>
    /// The points we're tracking (that we made in Start()) to make into a line strip.
    /// </summary>
    List<RectTransform> points = new List<RectTransform>();

    protected override void Start()
    {
        // Weird things can happen since we're derived off a Graphic
        if (Application.isPlaying == false)
            return;

        const float prepRad = 100.0f;
        const int pts = 8;
        for(int i = 0; i < pts; ++i)
        { 
            GameObject go = new GameObject();
            go.transform.SetParent(this.transform, false);

            UnityEngine.UI.Image img = go.AddComponent<UnityEngine.UI.Image>();
            img.rectTransform.sizeDelta = new Vector2(10.0f, 10.0f);
            UnityEngine.UI.Button btn = go.AddComponent<UnityEngine.UI.Button>();
            btn.targetGraphic = img;

            UnityEngine.EventSystems.EventTrigger et = go.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            et.triggers = new List<UnityEngine.EventSystems.EventTrigger.Entry>();
            //
            UnityEngine.EventSystems.EventTrigger.Entry e = new UnityEngine.EventSystems.EventTrigger.Entry();
            e.eventID = UnityEngine.EventSystems.EventTriggerType.Drag;
            e.callback.AddListener( (data)=>{ this.PointDrag( img.rectTransform, data); });
            et.triggers.Add(e);

            float th = (float)i/(float)pts * Mathf.PI * 2.0f;
            float x = Mathf.Cos(th) * prepRad;
            float y = Mathf.Sin(th) * prepRad;

            img.rectTransform.anchoredPosition = new Vector3(x, y, 0.0f);

            this.points.Add(img.rectTransform);
        }
    }

    /// <summary>
    /// Callback for when one of our UI vertices are dragged.
    /// </summary>
    /// <param name="rt">The RectTransform being dragged.</param>
    /// <param name="bed">The event data.</param>
    public void PointDrag(RectTransform rt, UnityEngine.EventSystems.BaseEventData bed)
    {
        UnityEngine.EventSystems.PointerEventData data = bed as UnityEngine.EventSystems.PointerEventData;
        this.SetVerticesDirty();

        rt.anchoredPosition += data.delta;
    }

    /// <summary>
    /// Virtual function to create the UI mesh.
    /// </summary>
    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
    {
        //base.OnPopulateMesh(vh);
        vh.Clear();

        // Weird things can happen since we're derived off a Graphic
        if(Application.isPlaying == false)
            return;

        // Combine the UI elements we're tracking into a line strip.
        List<Vector2> lineStrip = new List<Vector2>();
        foreach(RectTransform rt in this.points)
            lineStrip.Add(rt.anchoredPosition);

        List<Vector2> infs = GetInflationVectors(lineStrip);

        // Create the inflated mesh at the width the user specified from the control.
        CreateInflatedMesh(vh, lineStrip, infs, this.width, new Color(1.0f, 0.5f, 0.25f));

        // And then create an inflated line strip mesh on top that's just 1 radius thickness
        // that represents the unprocessed line.
        CreateInflatedMesh( vh, lineStrip, infs, 1.0f, Color.black);
    }

    /// <summary>
    /// Create an inflated mesh at a specified color and width.
    /// </summary>
    /// <param name="vh">The VertexHelpter from OnPopulateMesh().</param>
    /// <param name="lst">The line strip.</param>
    /// <param name="inf">The unit-inflation amount form line strip.</param>
    /// <param name="amt">The radius of how much to inflate the line strip.</param>
    /// <param name="c">The color of the inflated line mesh.</param>
    public static void CreateInflatedMesh(UnityEngine.UI.VertexHelper vh, List<Vector2> lst, List<Vector2> inf, float amt, Color c)
    { 
        int ct = vh.currentVertCount;

        // Add the positive and the negative side - this inflates the line on both
        // side and makes the inflation amount a radius that's half the actual width.
        for(int i = 0; i < lst.Count; ++i)
        { 
            UIVertex vt = new UIVertex();
            vt.position = lst[i] + inf[i] * amt;
            vt.color = c;

            UIVertex vb = new UIVertex();
            vb.position = lst[i] - inf[i] * amt;
            vb.color = c;

            vh.AddVert(vt);
            vh.AddVert(vb);
        }

        // Triangulate the vertices as quads.
        for(int i = 0; i < lst.Count - 1; ++i)
        { 
            int t0 = ct + i * 2 + 0;
            int t1 = ct + i * 2 + 1;
            int t2 = ct + i * 2 + 2;
            int t3 = ct + i * 2 + 3;

            vh.AddTriangle(t0, t1, t2);
            vh.AddTriangle(t1, t3, t2);
        }
    }

    /// <summary>
    /// Get the amount we need to extend per unit width.
    /// </summary>
    /// <param name="vecs">The line strip to process.</param>
    /// <returns>
    /// A 1-1 mapping of vectors for how much to move to inflate the vector
    /// 1 unit.</returns>
    public static List<Vector2> GetInflationVectors(List<Vector2> vecs)
    { 
        List<Vector2> ret = new List<Vector2>();

        ret.Add( PerpCCW((vecs[1] - vecs[0]).normalized));

        for(int i = 1; i < vecs.Count - 1; ++i)
        { 
            Vector2 toPt = PerpCCW((vecs[i] - vecs[i-1]).normalized);
            Vector2 frPt = PerpCW((vecs[i] - vecs[i+1]).normalized);
            Vector2 half = (toPt + frPt).normalized;
            float dot = Vector2.Dot(toPt, half);
            ret.Add((1.0f / dot) * half);
        }

        ret.Add(PerpCCW((vecs[vecs.Count - 1] - vecs[vecs.Count - 2]).normalized));
        return ret;
    }

    public void OnGUI()
    {
        float prevWidth = this.width;

        GUILayout.BeginHorizontal();
            GUILayout.Space(10.0f);
            GUILayout.BeginVertical(GUILayout.Width(300.0f));
                GUILayout.Space(10.0f);
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                    GUILayout.Label("Width", GUILayout.ExpandWidth(false));
                    this.width = GUILayout.HorizontalSlider(this.width, 1.0f, 50.0f, GUILayout.ExpandWidth(true));
                    GUILayout.Box(this.width.ToString("0.000"), GUILayout.Width(50.0f));
                GUILayout.EndHorizontal();

            GUILayout.Label("Click and drag points to reshape line strip.");
            GUILayout.Label("Use slider above to change inflation width.");

        if (GUILayout.Button("Reset Scene") == true)
                UnityEngine.SceneManagement.SceneManager.LoadScene(0);

            GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        if(this.width != prevWidth)
            this.SetVerticesDirty();

    }
}
