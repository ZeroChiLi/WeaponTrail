using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class WeaponTrailTest : MonoBehaviour
{
    public Animation animation;

    public Material[] materialArr;                  // 绘制的材质
    public Color color = Color.red;                 // 绘制的颜色
    public int sampleInterval = 3;                  // 采样帧间隔 0为每帧采样，1为每两帧采一次样
    public int totalSampleCount = 10;               // 采样总数
    public int totalSegCount = 20;                  // 分段总数
    public Transform point1;                        // 挂点1
    public Transform point2;                        // 挂点2
    public bool isEnableSmooth = true;                  // 启用平滑（平均分段）
    public bool isSmoothByNormal = true;                // 平滑顶部是根据 底部到顶部法线平均 计算得到
    public bool isSmoothByNormalAndStartWidth = true;   // 平滑顶部是根据 底部到顶部法线平均 和 起始长度 计算得到

    private Mesh _mesh;                             // 渲染网格
    private float _startWidth;                      // 起始长度
    private int _lastSampleCount;                   // 上一次分配的采样总数，重新初始化根据这个判断是否重新分配内存
    private int _lastSegCount;                      // 上一次分配的分段总数，重新初始化根据这个判断是否重新分配内存
    private int _lastSampleFrame = 0;               // 上一次采样的帧数
    private int _curSampleCount = 0;                // 当前采样总数

    private int _curStartPosIndex = -1;             // 当前采样到位置存在位置列表的索引
    private List<Vector3[]> _posList = new List<Vector3[]>();       // 所有采样的位置数据列表，根据采样索引 从后往前循环变量
    private float[] _distanceList;                  // 计算采样点距离差总数（累加）
    private Vector3[] _vertices;                    // 网格顶点列表
    private Vector2[] _uvs;                         // 网格UV列表
    private Color[] _colors;                        // 网格颜色列表
    private int[] _triangles;                       // 网格三角形列表

    private void Start()
    {
        animation?.Play();
        Init(totalSampleCount, totalSegCount);
    }

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="sampleCount">采样数</param>
    /// <param name="segCount">分段数</param>
    /// <returns></returns>
    private bool Init(int sampleCount, int segCount)
    {
        if (point1 == null || point2 == null)
        {
            Debug.LogError("初始化刀光：需要两个挂点！！");
            return false;
        }
        if (_mesh == null)
        {
            _mesh = new Mesh();
        }
        if (!InitMesh(sampleCount, segCount))
        {
            enabled = false;
            return false;
        }
        _startWidth = (point1.position - point2.position).magnitude;
        Debug.Log($"初始化刀光：sampleCount:{sampleCount}, segCount:{segCount}");
        return true;
    }

    /// <summary>
    /// 初始化网格
    /// </summary>
    /// <param name="sampleCount">采样数</param>
    /// <param name="segCount">分段数</param>
    /// <returns></returns>
    private bool InitMesh(int sampleCount, int segCount)
    {
        if (sampleCount <= 1)
        {
            Debug.LogError("初始化刀光：采样数至少大于等于2");
            return false;
        }
        if (segCount <= 1)
        {
            Debug.LogError("初始化刀光：网格段数至少大于等于2");
            return false;
        }

        if (_lastSampleCount != sampleCount) // 相同采样数不用重新分配
        {
            _lastSampleCount = sampleCount;
            // 位置列表，小于当前时添加新的，大于当前的移除
            var curPosCount = _posList.Count;
            if (curPosCount <= sampleCount)
            {
                for (int i = _posList.Count; i < sampleCount; i++)
                {
                    _posList.Add(new Vector3[2]);
                }
            }
            else
            {
                _posList.RemoveRange(sampleCount, curPosCount - sampleCount);
            }
            _distanceList = new float[sampleCount * 2];
        }

        if (_lastSegCount != segCount)  // 相同分段数不用重新分配
        {
            _lastSegCount = segCount;
            _vertices = new Vector3[_lastSegCount * 2];
            _uvs = new Vector2[_lastSegCount * 2];
            _colors = new Color[_lastSegCount * 2];
            _triangles = new int[(_lastSegCount - 1) * 6];
        }
        return true;
    }

    private void RefreshMesh()
    {
        ++_curSampleCount;
        Vector3[] curPos = null;
        var samplePosCount = _posList.Count;
        _curStartPosIndex = (_curStartPosIndex + 1) % samplePosCount;        // 取下一个采样点，循环取
        if (_curStartPosIndex < 0 || _curStartPosIndex >= samplePosCount)
        {
            Debug.LogError($"计算数据越界 _curStartIndex:{_curStartPosIndex}  count:{samplePosCount}");
            return;
        }
        curPos = _posList[_curStartPosIndex];       // 刷新采样点位置
        curPos[0] = point1.position;
        curPos[1] = point2.position;

        if (_curSampleCount < 2)  //至少凑够一个面
            return;


        var calcIndex = _curStartPosIndex;      // 起始位置 在位置点列表 的索引
        var len = _curSampleCount < samplePosCount ? _curSampleCount : samplePosCount;    // 一开始采样点是少于数组长度的
        _distanceList[0] = 0;               // 起始 顶部和底部点 累计距离为0
        _distanceList[1] = 0;
        var temIndex = -1;
        while (++temIndex < _lastSegCount)  // 刷新所有顶点数据
        {
            var verIndex = temIndex * 2;
            if (temIndex < len)
            {
                // 更新顶点位置
                _vertices[verIndex] = _posList[calcIndex][0];
                _vertices[verIndex + 1] = _posList[calcIndex][1];

                --calcIndex;
                if (calcIndex < 0)
                {
                    calcIndex = samplePosCount - 1;
                }
            }
            else    // 采样点少于数组长度，后面的顶点取最后一个有效顶点的位置
            {
                _vertices[verIndex] = _vertices[verIndex - 2];
                _vertices[verIndex + 1] = _vertices[verIndex - 1];
            }
            // 累计顶部点和底部点位移累计距离，后面平滑处理用
            if (temIndex > 0 && temIndex < samplePosCount)
            {
                _distanceList[verIndex] = Vector3.Magnitude(_vertices[verIndex] - _vertices[verIndex - 2]) + _distanceList[verIndex - 2];
                _distanceList[verIndex + 1] = Vector3.Magnitude(_vertices[verIndex + 1] - _vertices[verIndex - 1]) + _distanceList[verIndex - 1];
            }

            // 和下一个采样点位置 构建三角形
            if (temIndex < _lastSegCount - 1)
            {
                var tariangleIndex = temIndex * 6;
                _triangles[tariangleIndex] = verIndex;
                _triangles[tariangleIndex + 1] = verIndex + 1;
                _triangles[tariangleIndex + 2] = verIndex + 2;

                _triangles[tariangleIndex + 3] = verIndex + 1;
                _triangles[tariangleIndex + 4] = verIndex + 3;
                _triangles[tariangleIndex + 5] = verIndex + 2;
            }

            // 根据分段数 均分UV点
            _uvs[temIndex * 2].x = (float)temIndex / _lastSegCount;
            _uvs[temIndex * 2].y = 0;
            _uvs[temIndex * 2 + 1].x = (float)temIndex / _lastSegCount;
            _uvs[temIndex * 2 + 1].y = 1;

            // 填充颜色
            _colors[temIndex * 2] = color;
            _colors[temIndex * 2 + 1] = color;
        }

        // 是否启用平滑处理
        if (isEnableSmooth)
        {
            //var str = new StringBuilder();
            //var str2 = new StringBuilder();
            //for (int i = 0; i < _distanceList.Length - 2; i = i + 2)
            //{
            //    str.Append($"[{i / 2}={_distanceList[i]}],");
            //    str2.Append($"[{i / 2}={_distanceList[i + 1]}],");
            //}
            //Debug.Log("顶部点距离:" + str);
            //Debug.Log("底部点距离:" + str2);

            // 下一次查到最近距离点，用来优化循环查找次数，后面的距离一定是不小于上一次点的距离
            int findNextDistanceIndex = 0;
            var totoalTopDistance = _distanceList[samplePosCount * 2 - 2];      // 顶部总长度
            var totoalBottomDistance = _distanceList[samplePosCount * 2 - 1];   // 底部总长度
            var segIndex = -1;  // 段索引
            var realSegCount = _curSampleCount < _lastSegCount ? _curSampleCount : _lastSegCount;   // 实际段数量
            while (++segIndex < realSegCount)
            {
                var verIndex = segIndex * 2;
                var t = (float)segIndex / realSegCount;                     // 平均每个段距离
                var topDistance = t * totoalTopDistance;                    // 顶部插值的距离
                var bottomDistance = t * totoalBottomDistance;              // 底部插值的距离
                var topSegIndex = -1;                                       // 顶部找到的段索引
                var bottomSegIndex = -1;                                    // 底部找到的段索引
                var topLerpValue = 0f;                                      // 顶部找到所在段插值
                var bottomLerpValue = 0f;                                   // 底部找到所在段插值
                for (int i = findNextDistanceIndex; i < _distanceList.Length; i = i + 2)
                {
                    if (topSegIndex < 0 && _distanceList[i] >= topDistance)
                    {
                        topSegIndex = i / 2;
                        if (i >= 2)
                        {
                            var topSegDistance = _distanceList[i] - _distanceList[i - 2];
                            topLerpValue = (topDistance - _distanceList[i - 2]) / topSegDistance;
                        }
                    }
                    if (bottomSegIndex < 0 && _distanceList[i + 1] >= bottomDistance)
                    {
                        bottomSegIndex = i / 2;
                        if (i >= 2)
                        {
                            var bottomSegDistance = _distanceList[i + 1] - _distanceList[i - 1];
                            bottomLerpValue = (bottomDistance - _distanceList[i - 1]) / bottomSegDistance;
                        }
                    }
                    if (topSegIndex >= 0 && bottomSegIndex >= 0)
                    {
                        break;
                    }
                }
                if (topSegIndex == 0 || bottomSegIndex == 0)
                {
                    continue;
                }
                // 找不到 应该是算错误了 要检查一下
                if (topSegIndex < 0 || bottomSegIndex < 0)
                {
                    Debug.LogError($"刀光平滑插值段索引失败！topSegIndex:{topSegIndex}, bottomSegIndex:{bottomSegIndex}");
                    continue;
                }
                findNextDistanceIndex = (topSegIndex > bottomSegIndex ? bottomSegIndex : topSegIndex) * 2;

                // 计算到的顶部位置索引 和 前一个位置索引
                var topPosIndex = _curStartPosIndex - topSegIndex;
                if (topPosIndex < 0) topPosIndex += samplePosCount;
                var preTopPosIndex = (topPosIndex + 1) % samplePosCount;

                // 计算到的底部位置索引 和 前一个位置索引
                var bottomPosIndex = _curStartPosIndex - bottomSegIndex;
                if (bottomPosIndex < 0) bottomPosIndex += samplePosCount;
                var preBottomPosIndex = (bottomPosIndex + 1) % samplePosCount;
                
                // 最后插值当前段的位置
                var topFinal = Vector3.Lerp(_posList[preTopPosIndex][0], _posList[topPosIndex][0], topLerpValue);
                var bottomFinal = Vector3.Lerp(_posList[preBottomPosIndex][1], _posList[bottomPosIndex][1], bottomLerpValue);

                // 根据顶部点的采样点 和 前一个采样点的插值，得到一个插值的朝向
                var avgNormal = Vector3.Lerp((_posList[preTopPosIndex][0] - _posList[preTopPosIndex][1]), (_posList[topPosIndex][0] - _posList[topPosIndex][1]), topLerpValue);
                //Debug.Log($"调整平滑：avgNormal:{avgNormal} findNextDistanceIndex:{findNextDistanceIndex}\ntop:verIndex:{verIndex}  {_vertices[verIndex]} -> {topFinal} | {bottomFinal + avgNormal}, preTopPosIndex:{preTopPosIndex}, topPosIndex:{topPosIndex}, topLerpValue:{topLerpValue}" +
                //    $"\nbottom:verIndex:{verIndex + 1}  {_vertices[verIndex + 1]} -> {bottomFinal}, preBottomPosIndex:{preBottomPosIndex}, bottomPosIndex:{bottomPosIndex}, bottomLerpValue:{bottomLerpValue}");

                if (isSmoothByNormalAndStartWidth)
                {
                    // 根据插值的底部点 和 顶部的插值朝向 和 起始长度 算到一个点
                    _vertices[verIndex] = bottomFinal + avgNormal.normalized * Mathf.Lerp(_startWidth, avgNormal.magnitude, 0.2f);
                }
                else if (isSmoothByNormal)
                {
                    // 根据插值的底部点 和 顶部的插值朝向和长度
                    _vertices[verIndex] = bottomFinal + avgNormal;
                }
                else
                {
                    // 插值的顶部点
                    _vertices[verIndex] = topFinal;
                }
                // 插值的底部点
                _vertices[verIndex + 1] = bottomFinal;
            }
        }

        _mesh.Clear();
        _mesh.vertices = _vertices;
        _mesh.uv = _uvs;
        _mesh.colors = _colors;
        _mesh.triangles = _triangles;


    }

    private void Update()
    {
        // 检查采样数和分段不一致时，重新分配内存
        if (totalSampleCount != _lastSampleCount || totalSegCount != _lastSegCount)
        {
            Init(totalSampleCount, totalSegCount);
        }

        // 根据采样时间间隔刷新网格
        var curFrame = Time.frameCount;
        if (_lastSampleFrame + sampleInterval < curFrame)
        {
            RefreshMesh();
            _lastSampleFrame = curFrame;
        }

        // 给所有材质渲染一下
        for (int i = 0; i < materialArr.Length; i++)
        {
            if (materialArr[i] != null)
            {
                Graphics.DrawMesh(_mesh, Matrix4x4.identity, materialArr[i], 0, null, 0, null, false, false);
            }
        }
    }
}
