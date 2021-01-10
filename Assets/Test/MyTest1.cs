using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyTest1 : MonoBehaviour
{
    //public MeshFilter meshFilter;
    public Material material;
    public Color color = Color.red;
    public int sampleInterval = 3;
    public int maxSampleCount = 60;
    public Transform point1;
    public Transform point2;
    public bool isStart;
    public bool isStop;


    private Mesh _mesh;
    private bool _isPlaying;
    private int _curStartIndex = -1;
    private List<Vector3[]> _posList = new List<Vector3[]>();
    private int _lastSampleFrame = 0;
    private int _totalSampleCount = 0;
    private List<float[]> _distanceList = new List<float[]>();

    private Vector3[] _vertices;
    private Vector2[] _uvs;
    private Color[] _colors;
    private int[] _triangles;

    private void Start()
    {
        _mesh = new Mesh();
        if (!InitMesh())
        {
            enabled = false;
        }

    }

    private bool InitMesh()
    {
        if (maxSampleCount <= 1)
        {
            Debug.LogError("采样数至少大于等于2");
            return false;
        }
        _posList.Clear();
        for (int i = 0; i < maxSampleCount; i++)
        {
            _posList.Add(new Vector3[2]);
        }
        _vertices = new Vector3[maxSampleCount * 2];
        _uvs = new Vector2[maxSampleCount * 2];
        _colors = new Color[maxSampleCount * 2];
        _triangles = new int[(maxSampleCount - 1) * 6];
        return true;
    }

    private void RefreshMesh()
    {
        ++_totalSampleCount;
        Vector3[] curPos = null;
        var count = _posList.Count;
        _curStartIndex = (_curStartIndex + 1) % count;
        if (_curStartIndex < 0 || _curStartIndex >= count)
        {
            Debug.LogError($"计算数据越界 _curStartIndex:{_curStartIndex}  count:{count}");
            return;
        }
        curPos = _posList[_curStartIndex];
        curPos[0] = point1.position;
        curPos[1] = point2.position;

        if (_totalSampleCount < 2)  //至少凑够一个面
            return;
        _mesh.Clear();

        var temIndex = -1;
        var calcIndex = _curStartIndex;
        var len = _totalSampleCount < count ? _totalSampleCount : count;
        while (++temIndex < count)
        {
            var verIndex = temIndex * 2;
            if (temIndex < len)
            {
                _vertices[verIndex] = _posList[calcIndex][0];
                _vertices[verIndex + 1] = _posList[calcIndex][1];
            }
            else
            {
                _vertices[verIndex] = _posList[0][0];
                _vertices[verIndex + 1] = _posList[0][1];
            }

            if (temIndex < count - 1)
            {
                var tariangleIndex = temIndex * 6;
                _triangles[tariangleIndex] = verIndex;
                _triangles[tariangleIndex + 1] = verIndex + 1;
                _triangles[tariangleIndex + 2] = verIndex + 2;

                _triangles[tariangleIndex + 3] = verIndex + 1;
                _triangles[tariangleIndex + 4] = verIndex + 3;
                _triangles[tariangleIndex + 5] = verIndex + 2;
            }

            _uvs[temIndex * 2].x = (float)temIndex / count;
            _uvs[temIndex * 2].y = 0;
            _uvs[temIndex * 2 + 1].x = (float)temIndex / count;
            _uvs[temIndex * 2 + 1].y = 1;

            _colors[temIndex * 2] = color;
            _colors[temIndex * 2 + 1] = color;
            --calcIndex;
            if (calcIndex < 0)
            {
                calcIndex = count - 1;
            }
        }
        

        _mesh.vertices = _vertices;
        _mesh.uv = _uvs;
        _mesh.colors = _colors;
        _mesh.triangles = _triangles;


    }

    private void Update()
    {
        var curFrame = Time.frameCount;
        if (_lastSampleFrame + sampleInterval < curFrame)
        {
            RefreshMesh();
            _lastSampleFrame = curFrame;
        }

        Graphics.DrawMesh(_mesh, Matrix4x4.identity, material, gameObject.layer, null, 0, null, false, false);
    }
}
