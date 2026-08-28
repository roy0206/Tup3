using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AspectRatioEnforcer : Singleton<AspectRatioEnforcer>
{
    private const float TargetAspect = 16f / 9f;

    private Camera letterboxCamera;
    private int lastWidth;
    private int lastHeight;
    private int lastCameraCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        _ = Instance;
    }

    protected override void OnAwake()
    {
        CreateLetterboxClearCamera();
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        Apply();
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnActiveSceneChanged(Scene previous, Scene next)
    {
        Apply();
    }

    private void Update()
    {
        if (Screen.width != lastWidth
            || Screen.height != lastHeight
            || Camera.allCamerasCount != lastCameraCount)
        {
            Apply();
        }
    }

    private void CreateLetterboxClearCamera()
    {
        var go = new GameObject("LetterboxClearCamera");
        go.transform.SetParent(transform, false);

        letterboxCamera = go.AddComponent<Camera>();
        letterboxCamera.depth = -100f;
        letterboxCamera.clearFlags = CameraClearFlags.SolidColor;
        letterboxCamera.backgroundColor = Color.black;
        letterboxCamera.cullingMask = 0;
        letterboxCamera.orthographic = true;
        letterboxCamera.useOcclusionCulling = false;
        letterboxCamera.allowHDR = false;
        letterboxCamera.allowMSAA = false;
        letterboxCamera.rect = new Rect(0f, 0f, 1f, 1f);
    }

    private void Apply()
    {
        lastWidth = Screen.width;
        lastHeight = Screen.height;
        lastCameraCount = Camera.allCamerasCount;

        Rect target = ComputeTargetRect();
        foreach (var cam in Camera.allCameras)
        {
            if (cam == letterboxCamera) continue;
            cam.rect = target;
        }

        ConvertOverlayCanvases();
    }

    private readonly HashSet<int> convertedCanvasIds = new();

    private void ConvertOverlayCanvases()
    {
        var cam = Camera.main;
        if (cam == null) return;

        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!canvas.isRootCanvas) continue;
            if (canvas.GetComponent<ScreenFader>() != null) continue;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
                if (convertedCanvasIds.Add(canvas.GetInstanceID()))
                    canvas.sortingOrder += 100;
            }
            else if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null)
            {
                canvas.worldCamera = cam;
            }
        }
    }

    private static Rect ComputeTargetRect()
    {
        float current = Screen.height > 0 ? (float)Screen.width / Screen.height : TargetAspect;
        if (Mathf.Approximately(current, TargetAspect)) return new Rect(0f, 0f, 1f, 1f);

        if (current > TargetAspect)
        {
            float width = TargetAspect / current;
            return new Rect((1f - width) * 0.5f, 0f, width, 1f);
        }

        float height = current / TargetAspect;
        return new Rect(0f, (1f - height) * 0.5f, 1f, height);
    }
}

/* [파일 노트]
 * 화면비 16:9 고정(레터박스). PauseManager 와 같은 자동 부트스트랩 싱글톤이라 씬/프리팹 배치가
 * 필요 없다. 화면이 16:9 보다 넓으면 좌우(필러박스), 좁으면 상하(레터박스)에 검은 띠를 넣도록
 * 씬의 모든 카메라 rect 를 조정한다.
 *
 * - 검은 띠는 LetterboxClearCamera(depth -100, SolidColor 검정, cullingMask 0)가 매 프레임
 *   화면 전체를 지워서 만든다 — 카메라 rect 밖 영역은 아무도 클리어하지 않아 잔상이 남기 때문.
 * - 씬 전환·해상도 변경·카메라 개수 변화를 감지해 재적용한다.
 * - UI 도 16:9 안에 가두기 위해 씬/프리팹의 Overlay 루트 캔버스를 Apply 시점에
 *   Screen Space - Camera(Camera.main, planeDistance 1)로 자동 전환한다. 이때 카메라 공간에선
 *   월드 스프라이트와 sortingOrder 로 경쟁하므로 전환된 캔버스의 order 를 일괄 +100 시프트해
 *   (최초 1회, 상대 순서 유지) 스프라이트 위에 오도록 보장한다. 코드 생성 UI(UiViewBuilder)는
 *   생성 시점에 이미 카메라 모드로 만든다. 카메라가 없는 씬(Loading 등)은 Overlay 로 남는다.
 * - 예외: ScreenFader 는 의도적으로 Overlay 유지 — 씬 전환 중 카메라 교체 순간에도 페이드가
 *   끊기면 안 되고, 검은 띠까지 덮는 편이 자연스럽다.
 * - 대상 비율을 바꾸려면 TargetAspect 상수를 수정.
 */
