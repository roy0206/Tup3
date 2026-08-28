using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.U2D;

namespace SimWater
{
[RequireComponent(typeof(PolygonCollider2D))]
[RequireComponent(typeof(SpriteShapeController))]
public class Water : MonoBehaviour
{
    public Vector2 size;
    //public float nearZ;
    //public float farZ;
    
    public bool useSurface = true;
    
    private PolygonCollider2D _collider;
    private SpriteShapeController _spriteShape;
    private List<Vector2> _surface = new();
    
    private WaterSettings settings => WaterSettings.currentSettings;
    public Rect bounds => new ((Vector2)transform.position - new Vector2(size.x * 0.5f, size.y), size);
    public bool simulatable => 
        useSurface && bounds.SqrDistance(WaterSystem.simulationCenter) < settings.simulationDistance * settings.simulationDistance;

    public int maxNodeCount => Mathf.CeilToInt(size.x * settings.nodePerUnit) + 1;
    
    void Awake()
    {
        _collider = GetComponent<PolygonCollider2D>();
        _spriteShape = GetComponent<SpriteShapeController>();
        
    }

    void OnEnable()
    {
        WaterSystem.Register(this);
    }
    void OnDisable()
    {
        WaterSystem.Deregister(this);
    }

    void Update()
    {
        GetSurface();
        UpdateCollider(); 
        UpdateSpline();
    }

    void GetSurface()
    {
        var (xMin, xMax, yMax) = (bounds.xMin, bounds.xMax, bounds.yMax);
        _surface.Clear();
        if (useSurface&&WaterSystem.GetPositions(this, out NativeArray<float> positions, out RangeInt range) && range.length > 1)
        {
            var (start, end) = (range.start, range.end);
            if (start != 0) _surface.Add(new Vector2(xMin, yMax));
            for (int i = start; i < end; i++) _surface.Add(new Vector2(i / settings.nodePerUnit + xMin, yMax + positions[i - start]));
            if (_surface[^1].x > xMax) _surface[^1] = Vector2.Lerp(_surface[^2], _surface[^1], (xMax - _surface[^2].x) / (_surface[^1].x - _surface[^2].x));
        }
        else
        {
            _surface.Add(new Vector2(xMin, yMax));
            _surface.Add(new Vector2(xMax, yMax));
        }
    }

    void UpdateCollider()
    {
        var (px, py) = (transform.position.x,transform.position.y);
        var (xMin, xMax, yMin) = (-size.x * 0.5f, size.x * 0.5f, -size.y);
        List<Vector2> path = new List<Vector2>();
        foreach (var point in _surface)
        {
            path.Add(new Vector2(point.x  -px,point.y -py));
        }
        path.Add(new Vector2(xMax, yMin));
        path.Add(new Vector2(xMin, yMin));
        _collider.pathCount = 1;
        _collider.SetPath(0, path);
    }

    void UpdateSpline()
    {
        var (px, py) = (transform.position.x,transform.position.y);
        var spline = _spriteShape.spline;
        spline.Clear();
        spline.isOpenEnded = false;
        for (int i = 0; i < _surface.Count; i++)
        {
            spline.InsertPointAt(i, new Vector3(_surface[i].x-px, _surface[i].y-py, 0));
            spline.SetHeight(i , 0.2f);
            if (i==0 || i == _surface.Count - 1)
            {
                spline.SetTangentMode(i, ShapeTangentMode.Broken);
            }
            else
            {
                spline.SetTangentMode(i, ShapeTangentMode.Continuous);
                var t = 0.5f / settings.nodePerUnit * (_surface[i + 1] - _surface[i - 1]).normalized;
                spline.SetLeftTangent(i, -t);
                spline.SetRightTangent(i, t);
            }
        }
        spline.InsertPointAt(_surface.Count, new Vector3(size.x * 0.5f,-size.y,0));
        spline.SetTangentMode(_surface.Count, ShapeTangentMode.Broken);
        spline.SetHeight(_surface.Count, 0.0f);
        spline.InsertPointAt(_surface.Count + 1, new Vector3(-size.x * 0.5f,-size.y,0));
        spline.SetTangentMode(_surface.Count + 1, ShapeTangentMode.Broken);
        spline.SetHeight(_surface.Count + 1, 0.0f);

        _spriteShape.BakeMesh();
    }
    public float IndexToX(int index) => index / settings.nodePerUnit + bounds.xMin;
    public RangeInt OuterIndexRange(float min, float max)
    {
        int minIndex = Mathf.FloorToInt(settings.nodePerUnit * (min - bounds.xMin));
        int maxIndex = Mathf.CeilToInt(settings.nodePerUnit * (max - bounds.xMin)) + 1;
        minIndex = Mathf.Clamp(minIndex, 0, maxNodeCount);
        maxIndex = Mathf.Clamp(maxIndex, 0, maxNodeCount);
        return new RangeInt(minIndex, maxIndex - minIndex);
    }
    public RangeInt InnerIndexRange(float min, float max)
    {
        int minIndex = Mathf.CeilToInt(settings.nodePerUnit * (min - bounds.xMin));
        int maxIndex = Mathf.FloorToInt(settings.nodePerUnit * (max - bounds.xMin)) + 1;
        minIndex = Mathf.Clamp(minIndex, 0, maxNodeCount);
        maxIndex = Mathf.Clamp(maxIndex, 0, maxNodeCount);
        return new RangeInt(minIndex, maxIndex - minIndex);
    }
    void Reset()
    {
        _collider = GetComponent<PolygonCollider2D>();
        _spriteShape = GetComponent<SpriteShapeController>();
        _collider.isTrigger = true;
        _spriteShape.autoUpdateCollider = false;

        size = new(8, 6);
        OnValidate();
    }

    void OnValidate()
    {
        _collider = GetComponent<PolygonCollider2D>();
        _spriteShape = GetComponent<SpriteShapeController>();
        
        _surface.Clear();
        _surface.Add(new Vector2(bounds.xMin, bounds.yMax));
        _surface.Add(new Vector2(bounds.xMax, bounds.yMax));
        UpdateCollider();
        UpdateSpline();
    }
}
}

/* [파일 노트]
 * Tavern_Gamejam_CAU_SSU 프로젝트(Assets/Scripts/Water/Water.cs)에서 이식한 스프링 기반 2D 물 표면.
 * SpriteShapeController 스플라인과 PolygonCollider2D 를 매 프레임 시뮬레이션 표면에 맞춰 갱신한다.
 * 수정 사항: Tup3 의 수(水) 보스 클래스 Water 와 이름이 겹쳐 namespace SimWater 로 감쌌다(내용 동일).
 */