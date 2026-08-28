using System;
using UnityEngine;
using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Trees;
using System.Collections.Generic;
using DG.Tweening;

public abstract class BossBase : MonoBehaviour, IHealthUIEvent
{
    [SerializeField] protected BehaviorTree behaviorTree;
    [SerializeField] protected List<BoxCollider2D> boxColliders = new();
    [SerializeField] private float maxHp;

    [Header("피격 점멸")]
    [SerializeField] private bool enableHitFlash = true;
    [SerializeField] private Color hitFlashColor = Color.white;
    [SerializeField] private float hitFlashDuration = 0.12f;

    [Header("사운드")]
    [SerializeField] private string hitSoundName = "";
    [SerializeField] private string deathSoundName = "";
    [SerializeField] private float hitSoundVolume = 1f;
    [SerializeField] private float hitSoundMinInterval = 0.08f;
    [SerializeField] private float deathSoundVolume = 1f;

    protected AnimationController animationController;

    private float hp;
    public float Hp => hp;

    private bool isDead = false;
    public bool IsDead => isDead;

    private SpriteFlashGroup flashGroup;
    private bool flashCached;

    protected virtual string DefaultHitSoundName => string.Empty;

    protected virtual string DefaultDeathSoundName => string.Empty;

    protected virtual string CurrentHitSoundName => hitSoundName;

    protected float HitSoundVolume => hitSoundVolume;

    protected float HitSoundMinInterval => hitSoundMinInterval;

    protected void Awake()
    {
        hp = maxHp;

        if (string.IsNullOrWhiteSpace(hitSoundName)) hitSoundName = DefaultHitSoundName;
        if (string.IsNullOrWhiteSpace(deathSoundName)) deathSoundName = DefaultDeathSoundName;
    }

    public virtual bool DoDamage(float damage)
    {
        if(isDead) return false;
        hp -= damage;
        PlayHitFlash();
        PlayHitSound(CurrentHitSoundName);
        OnHealthChanged?.Invoke(hp, maxHp);
        Debug.Log($"<color=green>Boss Hit! {hp} Left</color>");
        if (hp <= 0)
        {
            isDead = true;
            BossSound.Play(deathSoundName, deathSoundVolume);
            OnDeath?.Invoke();
        }
        return true;
    }

    protected void PlayHitSound(string soundName)
    {
        BossSound.PlayThrottled(soundName, hitSoundVolume, hitSoundMinInterval);
    }

    protected virtual bool IsHitFlashRenderer(SpriteRenderer renderer)
    {
        return renderer != null;
    }

    protected Tween PlayHitFlash()
    {
        if (!enableHitFlash) return null;

        CacheFlashRenderers();
        if (flashGroup == null) return null;

        return flashGroup.Flash(hitFlashColor, Mathf.Max(0.01f, hitFlashDuration));
    }

    private void CacheFlashRenderers()
    {
        if (flashCached) return;
        flashCached = true;

        flashGroup = SpriteFlashGroup.GetOrAdd(gameObject);
        if (flashGroup == null) return;

        flashGroup.FlashColor = hitFlashColor;
        flashGroup.FlashDuration = Mathf.Max(0.01f, hitFlashDuration);
        flashGroup.Collect(IsHitFlashRenderer);
    }

    public event Action<float, float> OnHealthChanged;

    public event Action OnDeath;
}

/* [파일 노트]
 * OnDeath : hp 가 0 이하가 되는 순간 1회만 발생한다. DoDamage 는 맨 위에서 isDead 를 보고 즉시 return 하므로
 * 이미 죽은 보스를 또 때려도 다시 발생하지 않는다. 보스 파생 클래스(Soil/Water/Fire/Gold)는 손대지 않았고
 * BossBase 에 이벤트만 얹은 형태라, 기존 사망 연출(각 보스의 Dead() BT 태스크)과는 독립적으로 동작한다.
 * 즉 OnDeath 는 "체력이 0 이 된 시점"이지 "사망 연출이 끝난 시점"이 아니다.
 * BossRoom 은 이 차이를 메우려고 PostCutscene 상태에서 victoryDelay 만큼 기다렸다가 승리 대사를 띄운다.
 *
 * 피격 점멸 (4보스 + 최종보스 공통)
 *   DoDamage 안에서 PlayHitFlash() 를 1회 호출한다. 데미지 진입점이 여기 하나뿐이라
 *   개별 보스 파일을 고치지 않아도 전부 상속된다.
 *
 *   구현은 셰이더 기반 SpriteFlashGroup 이다. 예전에는 SpriteRenderer.color 를 흰색으로 바꾼 뒤
 *   DOColor 로 되돌렸는데, color 는 텍스처에 곱해지는 틴트라 원래 색이 흰색인 대부분의 스프라이트에서
 *   흰색을 곱해도 아무 변화가 없어 사실상 보이지 않았다. 지금은 SpriteFlash 시스템이
 *   Tup3/2D/Sprite Flash Lit 셰이더의 _FlashAmount 를 MaterialPropertyBlock 으로 밀어 넣어
 *   최종색을 lerp(원본, hitFlashColor, amount) 로 덮는다. 알파는 원본을 그대로 두므로 실루엣만 물든다.
 *   자세한 내용은 Assets/Scripts/InGame/Animation/SpriteFlashCore.cs 의 파일 노트를 볼 것.
 *
 *   인스펙터 필드 이름(enableHitFlash / hitFlashColor / hitFlashDuration)은 그대로 유지했다.
 *   씬·프리팹에 직렬화된 기존 값이 그대로 살아 있어야 하기 때문이다.
 *   PlayHitFlash 는 이제 Tween 을 반환하므로 파생 클래스가 .OnComplete() 등을 붙일 수 있다(써도 되고 안 써도 된다).
 *
 *   대상 렌더러는 첫 피격 때 1회만 모은다. 토보스처럼 본 리깅이라 자식 SR 이 여러 개여도
 *   트윈 하나가 전부를 함께 구동한다.
 *   연속 피격 시 이전 트윈을 Kill 한 뒤 다시 최대 강도부터 시작하므로 흰색으로 굳지 않는다.
 *
 *   제외 규칙은 IsHitFlashRenderer(virtual) 로 파생 클래스가 정한다. 기본은 "전부 포함"이고,
 *   FinalBoss 가 환영(soil/water/firePhantom)·거합 오버레이/섬광·검기 이펙트를 제외하도록 오버라이드한다
 *   (환영은 보스 자식이지만 별개 오브젝트이고 phantomAlpha 로 알파를 따로 관리하므로 같이 깜빡이면 안 된다).
 *   이 필터는 SpriteFlashGroup.Collect 에 그대로 넘어간다.
 *   LavaPool 의 굳음 DOColor 는 풀에서 꺼낸 전혀 다른 오브젝트라 이 수집에 들어오지 않는다.
 *
 * 피격음 / 사망음 (5보스 공통)
 *   DoDamage 안에서 PlayHitFlash 바로 뒤에 PlayHitSound(CurrentHitSoundName) 를 부른다.
 *   데미지 진입점이 여기 하나뿐이라 파생 보스를 고치지 않아도 전부 상속된다.
 *   보스마다 다른 소리를 쓸 수 있도록 두 겹으로 나뉘어 있다.
 *     - hitSoundName / deathSoundName : 인스펙터에서 보스별로 지정하는 값(빈 문자열이면 무음).
 *     - DefaultHitSoundName / DefaultDeathSoundName : 파생 클래스가 코드로 주는 기본값.
 *       Awake 에서 인스펙터 값이 비어 있을 때만 채우므로 인스펙터 지정이 항상 우선한다.
 *       (금보스·최종보스 = "Gold_HitLight", 토보스 사망 = "Soil_Death". 나머지는 파일이 없어 비워 둔다.)
 *     - CurrentHitSoundName : "지금 이 피격에 쓸 이름"을 파생 클래스가 상황에 따라 바꿀 수 있는 훅.
 *       금보스가 그로기 피격을 Gold_HitHeavy 로 갈라 쓰는 데 사용한다.
 *   연타 방지 : PlayHitSound 는 BossSound.PlayThrottled 를 거치며 hitSoundMinInterval(기본 0.08초)
 *   안에 같은 소리가 다시 울리지 않는다. 플레이어 3단 콤보처럼 짧은 간격의 연속 타격에서
 *   피격음이 겹쳐 뭉개지는 것을 막는다. 사망음은 1회뿐이라 스로틀 없이 재생한다.
 */
