# 보스 만들기 가이드 (BT 입문 + Soil 보스 해설)

## 1. BT(Behavior Tree)가 뭔가

**"매 프레임 위에서부터 아래로 훑으면서, 지금 할 수 있는 행동을 하나 골라 실행하는 트리"** 다.
`Update()`에서 `behaviorTree.Tick()`을 호출하면 루트부터 순회가 시작된다.

각 노드는 실행 결과로 3가지 중 하나를 돌려준다.

| 결과 | 의미 |
|---|---|
| `TaskStatus.Success` | 성공. 다음으로 넘어감 |
| `TaskStatus.Failure` | 실패. "지금은 못 해" |
| `TaskStatus.Continue` | 아직 진행 중. **다음 프레임에도 이 노드부터 다시 시작** |

노드 종류는 사실상 3개만 알면 된다.

- **Selector (OR)** — 자식을 위에서부터 시도, **하나라도 Success 나오면 멈춤**. 전부 실패하면 Failure.
  → "1번 패턴? 안 되면 2번? 안 되면 걷기?" 같은 **우선순위 선택**에 쓴다.
- **Sequence (AND)** — 자식을 위에서부터 실행, **하나라도 Failure면 즉시 중단**. 전부 성공해야 Success.
  → "쿨타임 됐나? 됐으면 공격한다" 같은 **조건 → 행동** 묶음에 쓴다.
- **Do (Action/Leaf)** — 실제 코드. `TaskStatus`를 반환하는 메서드를 넣는다.

우리는 [Fluid Behavior Tree](https://github.com/ashblue/fluid-behavior-tree) (`CleverCrow.Fluid.BTs`)를 쓰고,
에디터 그래프 없이 **코드로만** 트리를 만든다. `.End()`로 부모 노드를 닫고 `.Build()`로 마무리한다.

---

## 2. Soil 보스 트리 읽기

`Soil.cs`의 `Awake()`에 있는 트리:

```
Selector "Root"                       ← 위에서부터 하나 성공할 때까지
├─ Sequence "DeadSequence"
│   └─ Do "Dead"                      ← 죽었으면 사망 애니 재생하고 여기서 끝
├─ Selector "PatternSelector"         ← 패턴 1→2→3 순으로 시도
│   ├─ Sequence "1"  → Do Cool1 → Do A1   (Pattern1: 근접 3연타)
│   ├─ Sequence "2"  → Do Cool2 → Do A2   (Pattern2: 흙 낙하)
│   └─ Sequence "3"  → Do Cool3 → Do A3   (Pattern3: 기본 공격)
├─ Do "Go"                            ← 패턴 못 쓰면 플레이어 쪽으로 걷기
└─ Do "Stay"                          ← 사거리 안인데 쿨이면 대기
```

읽는 법: **위에 있을수록 우선순위가 높다.** 죽음 > 패턴 > 이동 > 대기.
`Cool1`이 Failure를 내면 Sequence "1"이 통째로 실패하고, Selector가 Sequence "2"로 넘어간다.

### 쿨타임 처리 (`curTimes`)

```csharp
private List<float> curTimes;   // [0]=현재 패턴 잠금, [1~3]=각 패턴 쿨타임
```

`Update()`에서 매 프레임 전부 `Time.deltaTime`만큼 깎는다.

- `curTimes[0]` — **패턴 지속시간 겸 전역 잠금**. 0보다 크면 패턴이 아직 연출 중이라는 뜻.
- `curTimes[1..3]` — 각 패턴 개별 쿨타임.

```csharp
private TaskStatus PatternStarter(int num)
{
    if (curTimes[num] > 0) return TaskStatus.Failure;          // 쿨 안 돎
    if (HorizontalDistance > attackRange) return TaskStatus.Failure; // 너무 멈
    return TaskStatus.Success;                                  // 발동!
}
```

### 패턴 하나의 구조 (전부 동일한 틀)

```csharp
private bool isParrernSetup;   // 이 패턴이 이미 시작됐는지 (전 패턴 공용 플래그)

private TaskStatus Pattern1()
{
    if (IsDead) return TaskStatus.Failure;

    if (!isParrernSetup)          // ── 진입 프레임에 딱 한 번 ──
    {
        curTimes[1] = 10;         // 이 패턴 쿨타임 10초
        curTimes[0] = 2;          // 이 패턴이 2초간 진행됨
        animationController.Play(1);
        isParrernSetup = true;

        // DOTween 타이머로 히트박스 on/off 타이밍 예약
        DOVirtual.DelayedCall(1f,   () => hitboxTransforms[0].gameObject.SetActive(true));
        DOVirtual.DelayedCall(0.5f, () => hitboxTransforms[0].gameObject.SetActive(false));
        ...
    }

    if (curTimes[0] > 0) return TaskStatus.Continue;  // 끝날 때까지 이 노드 붙잡기

    isParrernSetup = false;       // ── 종료 처리 ──
    return TaskStatus.Success;
}
```

핵심은 **`Continue`로 노드를 붙잡고 있는 동안 트리의 다른 가지가 실행되지 않는다**는 것.
그래서 패턴 중엔 걷기(`Go`)도 대기(`Stay`)도 안 튄다.

### 각 패턴이 하는 일

| | 쿨 | 지속 | 내용 |
|---|---|---|---|
| Pattern1 | 10s | 2s | `hitboxTransforms[0~2]`를 시차로 켰다 끄고, [2]는 `DOMoveX`로 앞으로 밀어 장판처럼 씀 |
| Pattern2 | 20s | 5s | `SoilDrop()` 코루틴 — 0.4초 간격 12번, 플레이어 방향 랜덤 X에 `SoilDrop` 프리팹을 풀에서 꺼내 위(y=5)에서 떨어뜨림. 4초 뒤 자동 반납 |
| Pattern3 | 0s | 2s | 기본 공격. `hitboxTransforms[3]`을 0.7s~1.0s 동안만 켬 |

Pattern3은 쿨이 0이라 **항상 통과되는 기본값** 역할이다. 그래서 Selector 맨 아래에 둔다.

### 이동 / 대기 / 중력

- `Move()` — 사거리 안이면 `Failure`(→ `Stay`로 넘어감), 밖이면 걷기 애니 + X축 이동 후 Success.
- `Stay()` — 대기 애니 + 플레이어 바라보기. 항상 Success (트리의 최종 fallback).
- `Face(dir)` — `localRotation` Y를 0/180으로 뒤집어 좌우 반전.
- `ApplyGravity()` — BT 밖, `Update()`에서 직접 호출. `BoxCast`로 접지 검사 후 수동 낙하.

---

## 3. 공용 부품

### `BossBase` (`BossBase.cs`)
모든 보스가 상속. `behaviorTree`, `boxColliders`, `maxHp`, `animationController`를 들고 있고
`DoDamage(float)` / `Hp` / `IsDead`를 제공한다. `Awake()`에서 `hp = maxHp`만 한다
→ **자식 클래스는 `new void Awake()`로 가리고 반드시 `base.Awake()`를 먼저 호출할 것.**

### `AnimationController` (`Animation/AnimationController.cs`)
레거시 `Animation` 컴포넌트 래퍼. 인스펙터의 `animationClips` 리스트 **인덱스**로 재생한다.
Soil 기준 관례: `0=사망, 1=패턴1, 2=패턴2, 3=패턴3, 4=대기, 5=이동`.

### `Hitbox` (`Hitbox.cs`)
`OnTriggerEnter2D`로 `PlayerHealth`를 찾아 `damage`를 넣는다. 기즈모로 콜라이더가 보인다.
**공격 판정은 전부 이 컴포넌트를 붙인 자식 오브젝트를 켰다 끄는 방식**이다.

### `PoolManager` (`Cores/PoolManager.cs`)
Addressables `Pool` 라벨로 프리팹을 미리 로드해두는 오브젝트 풀.
```csharp
var go = PoolManager.Instance.Get("SoilDrop", position, Quaternion.identity);
PoolManager.Instance.Release(go, 4f);   // 4초 뒤 자동 반납
```
투사체/장판 프리팹은 **Addressable로 만들고 `Pool` 라벨을 달아야** `Get`이 성공한다.
프리팹 이름이 곧 키이므로 이름은 유일해야 한다.

---

## 4. 새 보스 만드는 순서

1. **스크립트 생성** — `Boss/<이름>/<이름>.cs`, `public class Fire : BossBase`.
2. **Awake에서 트리 조립** — Soil 트리를 복붙하고 패턴 개수만 맞춘다.
   ```csharp
   new void Awake()
   {
       base.Awake();
       behaviorTree = new BehaviorTreeBuilder(gameObject)
           .Selector("Root")
               .Sequence("DeadSequence").Do("Dead", Dead).End()
               .Selector("PatternSelector")
                   .Sequence("1").Do("Cool1", () => PatternStarter(1)).Do("A1", Pattern1).End()
                   // 패턴 수만큼 반복
               .End()
               .Do("Go", Move)
               .Do("Stay", Stay)
           .End()
           .Build();
       curTimes = new List<float>() { 0, 0, 0, 0 };  // 패턴 수 + 1개
       animationController = GetComponent<AnimationController>();
       player = GameObject.FindGameObjectWithTag("Player");
       bodyCollider = boxColliders.Count > 0 ? boxColliders[0] : GetComponent<BoxCollider2D>();
   }
   ```
3. **`Update()`** — `curTimes` 감소 → `behaviorTree.Tick()` → `ApplyGravity()`.
4. **패턴 메서드 작성** — 위 "패턴 하나의 구조" 틀을 그대로 복사하고 안쪽 연출만 교체.
   쿨타임이 0인 기본 공격 하나를 **맨 아래**에 두면 보스가 멍때리지 않는다.
5. **프리팹 세팅**
   - 보스 오브젝트에 `Animation` + `AnimationController`(클립 순서 맞추기) + `BoxCollider2D`(몸통) 부착.
   - 공격 판정마다 자식 오브젝트 만들고 `Hitbox` + 트리거 콜라이더 부착 → **비활성 상태로 두고** `hitboxTransforms`에 순서대로 등록.
   - `maxHp`, `moveSpeed`, `attackRange`, `groundMask`(Ground 레이어) 채우기.
   - 투사체가 있으면 프리팹을 Addressable + `Pool` 라벨로 등록.

### 자주 하는 실수

- `.End()` 개수를 틀리면 트리 모양이 조용히 망가진다. 들여쓰기 맞춰서 세기.
- `isParrernSetup`은 **보스당 하나뿐인 공용 플래그**다. 패턴은 `Continue` 중에 서로 끼어들 수 없으니 지금은 문제 없지만, 패턴을 동시에 돌릴 생각이면 패턴별 플래그로 쪼개야 한다.
- `curTimes[0]`(지속시간)을 애니메이션 길이보다 짧게 잡으면 다음 행동이 애니를 끊는다.
- `Move()`/`Stay()`는 절대 `Failure`만 내면 안 된다. 둘 다 실패하면 보스가 아무것도 안 하는 프레임이 생긴다.
