# Portfolio

# Common
- 포트폴리오에 공통적으로 사용되는 스크립트 입니다.


  1. Inventory.
     - Inventory는 Asset Store의 [Recyclable Scroll Rect - Optimized List/Grid View(Free)] 에셋을 일부 수정하여 구현했습니다.
     - RecyclableItemViewer 및 RecyclableItem 스크립트를 상속하여 메일함과 같은 기능을 구현할 수 있습니다.
     - 데이터 필터링, 연속 PlayFab API 요청 및 응답대기, 아이템 간격 및 스크롤 바 등을 구현했습니다.


  2. StatData.
     - StatData는 Asset Store의 [Character Stats(Free)] 에셋을 일부 수정하여 구현했습니다.
     - 캐릭터, 장비, 인챈트가 공유하는 데이터로 Stat을 변경할 수 있습니다.
     - 데이터의 조립은 PlayFab 서버 사용을 전제로 조립합니다.
     - 간편한 StatMod 데이터의 사용을 위해 Reflection으로 StatModDictionary를 구성하여 사용합니다.


  3. DragonBonesUGUI.
     - DragonBones UGUI의 애니메이션 재생 및 장착한 장비 반영을 위한 스크립트입니다.


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
