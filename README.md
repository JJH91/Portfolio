# Portfolio

# Common
- 포트폴리오에 공통적으로 사용되는 스크립트 입니다.


  1. Inventory.
     - RecyclableItemViewer 및 RecyclableItem 스크립트를 상속하여 메일함과 같은 기능을 구현할 수 있습니다.
     - 데이터 필터링, 연속 PlayFab API 요청 및 응답대기, 아이템 간격 및 스크롤 바 등을 구현했습니다.
     - 무한 스크롤 뷰어는 Asset Store의 [Recyclable Scroll Rect - Optimized List/Grid View(Free)] 에셋을 일부 수정하여 사용했습니다.


  2. StatData.
     - 캐릭터, 장비, 인챈트가 공유하는 데이터로 Stat을 변경할 수 있습니다.
     - 데이터의 조립은 PlayFab 서버 사용을 전제로 조립합니다.
     - 간편한 StatMod 데이터의 사용을 위해 Reflection으로 StatModDictionary를 구성하여 사용합니다.
     - 스텟 모디파이 데이터는 Asset Store의 [Character Stats(Free)] 에셋을 일부 수정하여 사용했습니다.

  3. Skill.
     - 스킬은 3가지 스테이트가 순서대로 이루어져 있습니다. Projectile - Impact - Stream(Area).
     - 스킬의 종류에 따라 시작하는 스테이트는 다를 수 있지만, 순서는 바뀔 수 없습니다.
     - 스킬이 유닛과 콜라이더(트리거) 충돌하거나, 각 스테이트의 라이프 타임이 지나면 다음 스테이트로 변경됩니다.
     - 스킬은 데미지 계산 시에 시전 유닛과 피격 유닛의 데이터 참조가 유지되어야 하므로, 레퍼런스 카운트를 체크하여 null 오류가 발생하지 않도록 합니다.
     - 유도 스킬이라면, 유도 비율에 따라 방향을 수정하며 타깃에 접근합니다. 만약, 도중에 타깃이 없어지면 가장 가까운 타깃으로 방향을 수정합니다.

  4. DragonBonesUGUI.
     - DragonBones UGUI의 애니메이션 재생 및 장착한 장비 반영을 위한 스크립트입니다.


# Animal Defense(Team: Seond Run)
- Animal Defense 프로젝트에 사용된 스크립트 입니다.
- 본인 작업한 스크립트 일부를 업로드했습니다.


  1. Object Manager
     - NetworkAddressableMonoBehavior‎ 스크립트의 SetNetworkActive 메소드를 통해 Photon RPC가 실행되어 플레이어간 오브젝트의 활성/비활성화가 동기화되며 오브젝트 풀링에 사용됩니다.


  2. Unit(Character, Monster)
     - Character, Monster는 추상 클래스인 NetworkAddressableMonoBehavior‎를 상속받아 구현한 클래스입니다.
     - Character의 AI(FSM)는 해당 유닛을 생성한 클라이언트만 로직을 실행하고 필요한 부분만 RPC로 동기화하였습니다.
     - Monster의 이동을 PhotonView의 Transform 위치 동기화만 사용할 경우, 네트워크 환경에 따라 부자연스럽게 움직이는 경우가 있었습니다. 보간을 통해 자연스러운 움직임을 구현할 수도 있지만, 생성 위치만 동기화하고 이동은 각 클라이언트에서 처리하여 네트워크 전송량을 줄이고 좀 더 자연스러운 움직임을 보이도록 했습니다.


  3. Skill
     - Skill은 추상 클래스인 NetworkAddressableMonoBehavior‎를 상속받아 구현한 클래스입니다.
     - Skill의 발동은 RPC로 동기화하였습니다. 여기서 발동한 캐릭터와 타겟 몬스터의 정보를 파라미터로 넘겨주어야 하는데, Photon에서 네트워크 전송을 지원하는 데이터 타입으로 전송이 필요했습니다. 각 유닛의 PhotonView ID값(int)을 전송하고 CombatManager에서 이 값으로 Dictionary에서 유닛을 접근하는 방법을 사용해 문제를 해결했습니다.
     - Skill의 충돌 처리는 각 스킬을 생성한 클라이언트에서 처리한 후 동기화시켜 플레이어의 플레이 경험을 개선하였습니다.


# Slime Hill
- Slime Hill 프로젝트에 사용된 스크립트 입니다.


  1. DragonBonesUnit, Character, Monster.
     - Character, Monster는 추상 클래스인 DragonBones Unit을 상속받아 구현한 유닛 클래스입니다.
     - 각 유닛에 맞는 애니메이션 재생이나 이동, 장비 교체, 프레임 이벤트 수신 등의 기능을 합니다.


  2. Map.
     - SpawnLocator로 유닛 소환, WayPointer로 유닛의 이동, NavMesh로 NavMeshAgent가 맵을 이동할 수 있습니다.


# Dunsland
- Dunsland 프로젝트에 사용된 스크립트 입니다.


  1. DragonBonesUnit, Character, Monster.
     - Character, Monster는 추상 클래스인 DragonBones Unit을 상속받아 구현한 유닛 클래스입니다.
     - 각 유닛에 맞는 애니메이션 재생이나 이동, 장비 교체, 프레임 이벤트 수신 등의 기능을 합니다.


  2. Map.
     - SpawnLocator로 유닛 소환을, NavMesh로 NavMeshAgent가 맵을 이동할 수 있습니다.
     - 대체 소환 기능을 사용하여 맵에 등장하는 유닛 역할 군을 자유롭게 배치할 수 있습니다.


  3. SkillWarningEffect.
     - 계층구조를 가진 스킬의 콜라이더 범위를 그리는 스크립트입니다. 부모와 자식(콜라이더)의 스케일, 회전을 반영합니다.
     - 콜라이더의 CreateMesh 메소드는 계층 구조를 가진 스킬의 부모와 자식(콜라이더) 스케일과 회전이 각각 다를 때 실제 콜라이더 범위를 나타내지 않아 스크립트를 만들었습니다.


  4. UnitSettingManager.
     - 사용자가 유닛의 장비를 편집할 때, 변경 사항을 모아서 서버에 보내어 API Call을 최소화합니다.
     - 장비 편집 데이터를 클라이언트 내부에서 데이터를 저장하여 장비 편집 도중에 데이터가 날아가는 경우를 미연에 방지, 사용자가 겪는 불편함을 최소화했습니다.


  5. WeaponManager, Weapon
     - 유닛이 사용하는 장비를 보여주기 위한 Weapon과 사용자가 공격 버튼을 연속적으로 연타해도 무기의 RPM이 유지되도록 관리하는 WeaponManager 스크립트입니다.
     - WeaponManager는 무기의 시리즈에 따라 일반 공격시에 추가적으로 스킬을 발동하는 기능도 합니다.
