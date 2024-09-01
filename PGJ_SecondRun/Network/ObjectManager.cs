using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Photon.Pun;

public class ObjectManager : MonoSingleton<ObjectManager>
{
    // 카테고리 추가시에 추가 필수
    public enum Category
    {
        Loading,
        Character, Monster,
        Weapon,
        Skill, Effect,
        WorldUI
    }

    // Prefab asset handle dictionary.
    [SerializeField] Dictionary<Category, Dictionary<string, object>> allPrefabAssetHandleDictionary;
    [SerializeField] Dictionary<Category, Dictionary<string, object>> allNetworkPrefabAssetHandleDictionary;
    [SerializeField] Dictionary<Category, Dictionary<string, object>> loadingPrefabAssetHandleDictionary;

    // Asset handle dictionary.
    [SerializeField] Dictionary<string, object> allAssetHandleDictionary;

    // 오브젝트 하이어라키 정렬용
    Dictionary<Category, Transform> categoryDictionary = new Dictionary<Category, Transform>();
    Dictionary<Category, Dictionary<string, Transform>> prefabGroupDictionary = new Dictionary<Category, Dictionary<string, Transform>>();

    event Action ClearPrefabHandlesAct;
    // public event Action<IUsingAddressabledAsset> DereferencingAssetHandleAct;

    private void Awake()
    {
        MakeDestroyableInstance();

        allPrefabAssetHandleDictionary = new Dictionary<Category, Dictionary<string, object>>();
        allNetworkPrefabAssetHandleDictionary = new Dictionary<Category, Dictionary<string, object>>();
        loadingPrefabAssetHandleDictionary = new Dictionary<Category, Dictionary<string, object>>();
        allAssetHandleDictionary = new Dictionary<string, object>();
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *               * Clear Asset Handle *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public void ClearAllPrefabAssets()
    {
        // 오브젝트 모두 파괴 및 핸들 해제
        ClearPrefabHandlesAct?.Invoke();
        ClearPrefabHandlesAct = null;

        allPrefabAssetHandleDictionary.Clear();
        allNetworkPrefabAssetHandleDictionary.Clear();
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *               * Check Handle Exist *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    bool CheckHandleExist(Category category, string prefabName)
    {
        if (!allPrefabAssetHandleDictionary.ContainsKey(category))
            allPrefabAssetHandleDictionary.Add(category, new Dictionary<string, object>());

        return !allPrefabAssetHandleDictionary[category].ContainsKey(prefabName);
    }

    bool CheckNetworkHandleExist(Category category, string prefabName)
    {
        if (!allNetworkPrefabAssetHandleDictionary.ContainsKey(category))
            allNetworkPrefabAssetHandleDictionary.Add(category, new Dictionary<string, object>());

        return !allNetworkPrefabAssetHandleDictionary[category].ContainsKey(prefabName);
    }

    bool CheckLoadingHandleExist(Category category, string prefabName)
    {
        if (!loadingPrefabAssetHandleDictionary.ContainsKey(category))
            loadingPrefabAssetHandleDictionary.Add(category, new Dictionary<string, object>());

        return !loadingPrefabAssetHandleDictionary[category].ContainsKey(prefabName);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *         * Basic Prefab Asset Handle Maker *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    void MakePrefabHandle<T>(Category category, string prefabName, bool hasAddressableAssetChild, int qSize = 0) where T : Component, IAddressable
    {
        // IAdressable Prefab 을 자식 오브젝트로 보유하고 있으면, 부모 프리펩이 파괴될때 하위 프리펩들이 오브젝트 풀링이 안되므로 (강제로 파괴됨)
        // 부모 프리펩이 파괴되지 않도록 qSize 를 무한으로 한다.
        if (hasAddressableAssetChild)
            qSize = -1;

        var prefabAssetHandle = new PrefabAssetHandle<T>(prefabName, qSize, GetPrefabGroup(category, prefabName));

        // 프리펩 에셋 핸들을 딕셔너리에 새로 할당한다,
        allPrefabAssetHandleDictionary[category].Add(prefabName, prefabAssetHandle);
    }

    void MakeLoadingPrefabHandle<T>(Category category, string prefabName, int qSize = 1) where T : Component, IAddressable
    {
        var loadingPrefabAssetHandle = new PrefabAssetHandle<T>(prefabName, qSize, GetPrefabGroup(category, prefabName));

        // 프리펩 에셋 핸들을 딕셔너리에 새로 할당한다,
        loadingPrefabAssetHandleDictionary[category].Add(prefabName, loadingPrefabAssetHandle);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                * Get Asset Handle *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    public AssetHandle<T> GetAssetHandle<T>(string assetName, IUsingAddressabledAsset usingAddressabledAsset) where T : class
    {
        if (!allAssetHandleDictionary.ContainsKey(assetName))
            allAssetHandleDictionary.Add(assetName, new AssetHandle<T>(assetName));

        return (allAssetHandleDictionary[assetName] as AssetHandle<T>).GetAssetHande(usingAddressabledAsset);
    }

    public AssetHandle<SpriteAtlas> GetAtlasAssetHandle(string atlasName, IUsingAddressabledAsset usingAddressabledAsset)
    {
        if (!allAssetHandleDictionary.ContainsKey(atlasName))
            allAssetHandleDictionary.Add(atlasName, new AssetHandle<SpriteAtlas>(atlasName));

        return (allAssetHandleDictionary[atlasName] as AssetHandle<SpriteAtlas>).GetAssetHande(usingAddressabledAsset);
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *    * Category And Object Group Search / Create *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    Transform GetPrefabGroup(Category category, string prefabName)
    {
        Transform categoryTr;

        if (categoryDictionary.ContainsKey(category))
            categoryTr = categoryDictionary[category];
        else
        {
            categoryTr = new GameObject().transform;
            // ! 수정
            // categoryTr.name = $"{category.ToCachedUncamelCaseString()} Category";
            categoryTr.name = $"{category} Category";
            categoryTr.SetParent(transform);

            categoryDictionary.Add(category, categoryTr);
        }

        if (!prefabGroupDictionary.ContainsKey(category))
            prefabGroupDictionary.Add(category, new Dictionary<string, Transform>());

        if (prefabGroupDictionary[category].ContainsKey(prefabName))
            return prefabGroupDictionary[category][prefabName];
        else
        {
            var prefabGroupTr = new GameObject().transform;
            prefabGroupTr.name = prefabName + " Group";
            prefabGroupTr.SetParent(categoryTr);

            prefabGroupDictionary[category].Add(prefabName, prefabGroupTr);

            return prefabGroupTr;
        }
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                   * Make Unit *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    /**
     * --------------------------------------------------
     *               * 몬스터 생성 메소드 *
     * --------------------------------------------------
     */

    void MakeCharacterNetworkPrefabHandle(Category category, string characterName, int qSize = 50)
    {
        var prefabAssetHandle = new NetworkPrefabAssetHandle<Character>(characterName, qSize, GetPrefabGroup(category, characterName));

        // 프리펩 에셋 핸들을 딕셔너리에 새로 할당한다,
        allNetworkPrefabAssetHandleDictionary[category].Add(characterName, prefabAssetHandle);
    }

    public Character GetCharacter(string characterName, Vector2 position, Category category = Category.Character)
    {
        if (CheckNetworkHandleExist(category, characterName))
            MakeCharacterNetworkPrefabHandle(category, characterName);

        var networkPrefabAssetHandle = allNetworkPrefabAssetHandleDictionary[category][characterName] as NetworkPrefabAssetHandle<Character>;
        var character = networkPrefabAssetHandle.GetPrefabAsset(position, NetworkManager.Instance.IsMasterClient);

        character.transform.position = position;

        character.InitUnit();

        return character;
    }

    void MakeMonsterNetworkPrefabHandle(Category category, string monsterName, int qSize = 300)
    {
        var prefabAssetHandle = new NetworkPrefabAssetHandle<Monster>(monsterName, qSize, GetPrefabGroup(category, monsterName));

        // 프리펩 에셋 핸들을 딕셔너리에 새로 할당한다,
        allNetworkPrefabAssetHandleDictionary[category].Add(monsterName, prefabAssetHandle);
    }

    public Monster GetMonster(string monsterName, Vector2 position, Category category = Category.Monster)
    {
        if (CheckNetworkHandleExist(category, monsterName))
            MakeMonsterNetworkPrefabHandle(category, monsterName);

        var networkPrefabAssetHandle = allNetworkPrefabAssetHandleDictionary[category][monsterName] as NetworkPrefabAssetHandle<Monster>;
        var monster = networkPrefabAssetHandle.GetPrefabAsset(position, NetworkManager.Instance.IsMasterClient);

        monster.transform.position = position;

        monster.InitUnit();

        return monster;
    }

    /**
     * --------------------------------------------------
     *                * 스킬 생성 메소드 *
     * --------------------------------------------------
     */

    void MakeSkillNetworkPrefabHandle(Category category, SkillData_SO skillData_SO, int qSize = 100)
    {
        var prefabAssetHandle = new NetworkPrefabAssetHandle<Skill>(skillData_SO.SkillName, qSize, GetPrefabGroup(category, skillData_SO.SkillName));

        // 프리펩 에셋 핸들을 딕셔너리에 새로 할당한다,
        allNetworkPrefabAssetHandleDictionary[category].Add(skillData_SO.SkillName, prefabAssetHandle);
    }

    public Skill GetSkill(SkillData_SO skillData_SO, Vector2 position, Category category = Category.Skill)
    {
        if (CheckNetworkHandleExist(category, skillData_SO.SkillName))
            MakeSkillNetworkPrefabHandle(category, skillData_SO);

        var prefabAssetHandle = allNetworkPrefabAssetHandleDictionary[category][skillData_SO.SkillName] as NetworkPrefabAssetHandle<Skill>;
        var skill = prefabAssetHandle.GetPrefabAsset(position, NetworkManager.Instance.IsMasterClient);

        return skill;
    }

    /**
     * --------------------------------------------------
     *               * 이펙트 생성 메소드 *
     * --------------------------------------------------
     */

    public Effect GetEffect(string prefabName, Vector3 position, Category category = Category.Effect)
    {
        if (CheckHandleExist(category, prefabName))
            MakePrefabHandle<Effect>(category, prefabName, false, 10);

        var prefabAssetHandle = allPrefabAssetHandleDictionary[category][prefabName] as PrefabAssetHandle<Effect>;
        var effect = prefabAssetHandle.GetPrefabAsset();
        effect.transform.position = position;

        return effect;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *                    * Make UI *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    /**
     * --------------------------------------------------
     *               * 로딩 UI 생성 메소드 *
     * --------------------------------------------------
     */

    public SceneLoadingUI GetSceneLoadingUI()
    {
        var prefabName = "Scene Loading UI";
        var category = Category.Loading;

        if (CheckLoadingHandleExist(category, prefabName))
            MakeLoadingPrefabHandle<SceneLoadingUI>(category, prefabName);

        var loadingPrefabAssetHandle = loadingPrefabAssetHandleDictionary[category][prefabName] as PrefabAssetHandle<SceneLoadingUI>;

        return loadingPrefabAssetHandle.GetPrefabAsset();
    }

    /**
     * --------------------------------------------------
     *             * 데미지 표시 생성 메소드 *
     * --------------------------------------------------
     */

    public DamageIndicator GetDamageIndicator(Category category = Category.WorldUI)
    {
        string prefabName = "Damage Indicator";

        if (CheckHandleExist(category, prefabName))
            MakePrefabHandle<DamageIndicator>(category, prefabName, false, -1);

        var prefabAssetHandle = allPrefabAssetHandleDictionary[category][prefabName] as PrefabAssetHandle<DamageIndicator>;
        var damageIndicator = prefabAssetHandle.GetPrefabAsset();

        return damageIndicator;
    }

    /**
     * !--------------------------------------------------
     * !--------------------------------------------------
     *               * Asset Handle Class *
     * ?--------------------------------------------------
     * ?--------------------------------------------------
     */

    // 프리펩 에셋 핸들.
    [Serializable]
    public class PrefabAssetHandle<T> where T : Component, IAddressable
    {
        string assetAddress;
        AsyncOperationHandle<GameObject> handle;
        [SerializeField] bool isHandleLoaded;
        [SerializeField] List<GameObject> refPrefabList = new List<GameObject>();
        [SerializeField] Queue<T> queue = new Queue<T>();
        [SerializeField] int instNum;
        [SerializeField] int refCount;
        [SerializeField] int qSize;
        [SerializeField] Transform prefabGroup;
        [SerializeField] public T original;

        public PrefabAssetHandle(string assetAddress, int qSize, Transform prefabGroup)
        {
            this.assetAddress = assetAddress;
            this.qSize = qSize;
            this.prefabGroup = prefabGroup;
        }

        // 클리어가 작동되면 모든 프리펩을 파괴하고 핸들을 해제함.
        public void AssetHandleClear()
        {
            queue.Clear();

            if (refPrefabList.Count > 0)
                for (int i = refPrefabList.Count - 1; i >= 0; i--)
                    if (refPrefabList[i] != null)
                        Destroy(refPrefabList[i]);
            refPrefabList.Clear();

            if (isHandleLoaded)
            {
                Addressables.Release(handle);
                isHandleLoaded = false;
            }
        }

        void LoadAssetHandle()
        {
            if (!isHandleLoaded)
            {
                handle = Addressables.LoadAssetAsync<GameObject>(assetAddress);
                handle.Completed +=
                    (_handle) =>
                    {
                        isHandleLoaded = true;
                    };
                handle.WaitForCompletion();

                // 로딩 UI 는 씬 이동시에 파괴가 되면 안 됨.
                if (prefabGroup.parent.name != $"{Category.Loading} Category")
                    Instance.ClearPrefabHandlesAct += AssetHandleClear;
            }
        }

        public T GetPrefabAsset(bool isStackOnQueue = false)
        {
            LoadAssetHandle();

            if (isStackOnQueue || queue.Count == 0)
                return CreateNewPrefab(isStackOnQueue);
            else
            {
                var prefab = queue.Dequeue();
                prefab.IsEnqueued = false;
                prefab.gameObject.SetActive(true);

                return prefab;
            }
        }

        T CreateNewPrefab(bool isStackOnQueue = false)
        {
            refCount++;
            instNum++;

            T newPrefab = null;

            newPrefab = Instantiate(handle.Result, Vector3.zero, Quaternion.identity, prefabGroup).GetComponent<T>();

            newPrefab.name += instNum.ToString();

            refPrefabList.Add(newPrefab.gameObject);
            if (isStackOnQueue) // Stack on queue.
            {
                newPrefab.gameObject.SetActive(false);
                queue.Enqueue(newPrefab);
                newPrefab.IsEnqueued = true;
            }

            // Subscribe object pooling event.
            newPrefab.SetParentToObjectPoolAct += SetParentToObjectPool;
            newPrefab.OnAssetEnabledAct += OnAssetEnabled;
            newPrefab.OnAssetDisabledAct += OnAssetDisabled;
            newPrefab.OnAssetDestroyedAct += OnDestroyed;

            return newPrefab;
        }

        void SetParentToObjectPool(object prefab)
        {
            var castedPrefab = prefab as T;

            castedPrefab.transform.SetParent(prefabGroup);
            castedPrefab.transform.localPosition = Vector3.zero;
        }

        void OnAssetEnabled(object prefab)
        {
            var castedPrefab = prefab as T;

            if (queue.Contains(castedPrefab))
            {
                castedPrefab.gameObject.SetActive(false);
            }
        }

        void OnAssetDisabled(object prefab)
        {
            var castedPrefab = prefab as T;

            if (queue.Count < qSize || qSize < 0)
            {
                queue.Enqueue(castedPrefab);
                castedPrefab.IsEnqueued = true;
            }
            else
            {
                Destroy(castedPrefab.gameObject);
            }
        }

        void OnDestroyed(object prefab)
        {
            var castedPrefab = prefab as T;

            refCount--;
            refPrefabList.Remove(castedPrefab.gameObject);

            if (castedPrefab.IsEnqueued && queue.Count > 0)
                queue = new Queue<T>(queue.Where(prefab => prefab != castedPrefab));

            if (refCount == 0)
            {
                Instance.ClearPrefabHandlesAct -= AssetHandleClear;
                AssetHandleClear();
            }
        }
    }

    // 네트워크 프리펩 에셋 핸들.
    [Serializable]
    public class NetworkPrefabAssetHandle<T> where T : NetworkAddressableMonoBehavior
    {
        string assetAddress;
        [SerializeField] List<GameObject> refPrefabList = new List<GameObject>();
        [SerializeField] Queue<T> queue_p1 = new Queue<T>();
        [SerializeField] Queue<T> queue_p2 = new Queue<T>();
        [SerializeField] int instNum;
        [SerializeField] int qSize;
        [SerializeField] Transform prefabGroup;
        [SerializeField] public T original;

        public NetworkPrefabAssetHandle(string assetAddress, int qSize, Transform prefabGroup)
        {
            this.assetAddress = assetAddress;
            this.qSize = qSize;
            this.prefabGroup = prefabGroup;

            Instance.ClearPrefabHandlesAct += AssetClear;
        }

        // 클리어가 작동되면 모든 프리펩을 파괴.
        public void AssetClear()
        {
            queue_p1.Clear();
            queue_p2.Clear();

            if (refPrefabList.Count > 0)
                for (int i = refPrefabList.Count - 1; i >= 0; i--)
                    if (refPrefabList[i] != null)
                        PhotonNetwork.Destroy(refPrefabList[i]);
            refPrefabList.Clear();

            Debug.Log($"에셋 클리어");
        }

        public T GetPrefabAsset(Vector3 position, bool isMasterClient, bool isStackOnQueue = false)
        {
            if (isMasterClient)
            {
                if (isStackOnQueue || queue_p1.Count == 0)
                    return CreateNewPrefab(position, isMasterClient, isStackOnQueue);
                else
                {
                    var prefab = queue_p1.Dequeue();
                    prefab.IsEnqueued = false;
                    prefab.SetNetworkActive(true, position);

                    return prefab;
                }
            }
            else
            {
                if (isStackOnQueue || queue_p2.Count == 0)
                    return CreateNewPrefab(position, isMasterClient, isStackOnQueue);
                else
                {
                    var prefab = queue_p2.Dequeue();
                    prefab.IsEnqueued = false;
                    prefab.SetNetworkActive(true, position);

                    return prefab;
                }
            }
        }

        T CreateNewPrefab(Vector3 position, bool isMasterClient, bool isStackOnQueue = false)
        {
            instNum++;

            T newPrefab = null;

            newPrefab = PhotonNetwork.Instantiate(assetAddress, position, Quaternion.identity).GetComponent<T>();
            newPrefab.transform.SetParent(prefabGroup);

            newPrefab.name += instNum.ToString();

            refPrefabList.Add(newPrefab.gameObject);
            if (isStackOnQueue) // Stack on queue.
            {
                newPrefab.SetNetworkActive(false, position);
                if (isMasterClient)
                    queue_p1.Enqueue(newPrefab);
                else
                    queue_p2.Enqueue(newPrefab);
                newPrefab.IsEnqueued = true;
            }

            // Subscribe object pooling event.
            newPrefab.SetParentToObjectPoolAct += SetParentToObjectPool;
            newPrefab.OnAssetEnabledAct += OnAssetEnabled;
            newPrefab.OnAssetDisabledAct += OnAssetDisabled;
            newPrefab.OnAssetDestroyedAct += OnDestroyed;

            return newPrefab;
        }

        void SetParentToObjectPool(object prefab)
        {
            var castedPrefab = prefab as T;

            castedPrefab.transform.SetParent(prefabGroup);
            castedPrefab.transform.localPosition = Vector3.zero;
        }

        void OnAssetEnabled(object prefab, bool isMasterClient)
        {
            var castedPrefab = prefab as T;

            if (isMasterClient)
            {
                if (queue_p1.Contains(castedPrefab))
                {
                    castedPrefab.SetNetworkActive(false, castedPrefab.transform.position);
                }
            }
            else
            {
                if (queue_p2.Contains(castedPrefab))
                {
                    castedPrefab.SetNetworkActive(false, castedPrefab.transform.position);
                }
            }
        }

        void OnAssetDisabled(object prefab, bool isMasterClient)
        {
            var castedPrefab = prefab as T;

            if (isMasterClient)
            {
                if (queue_p1.Count < qSize || qSize < 0)
                {
                    queue_p1.Enqueue(castedPrefab);
                    castedPrefab.IsEnqueued = true;
                }
                else
                {
                    PhotonNetwork.Destroy(castedPrefab.gameObject);
                }
            }
            else
            {
                if (queue_p2.Count < qSize || qSize < 0)
                {
                    queue_p2.Enqueue(castedPrefab);
                    castedPrefab.IsEnqueued = true;
                }
                else
                {
                    PhotonNetwork.Destroy(castedPrefab.gameObject);
                }
            }
        }

        void OnDestroyed(object prefab, bool isMasterClient)
        {
            var castedPrefab = prefab as T;

            refPrefabList.Remove(castedPrefab.gameObject);

            if (isMasterClient)
            {
                if (castedPrefab.IsEnqueued && queue_p1.Count > 0)
                    queue_p1 = new Queue<T>(queue_p1.Where(prefab => prefab != castedPrefab));
            }
            else
            {
                if (castedPrefab.IsEnqueued && queue_p2.Count > 0)
                    queue_p2 = new Queue<T>(queue_p2.Where(prefab => prefab != castedPrefab));
            }
        }
    }

    // 프리펩이 아닌 에셋 핸들.
    [Serializable]
    public class AssetHandle<T> where T : class
    {
        AsyncOperationHandle<T> handle;
        public string AssetAddress { get; private set; }
        [SerializeField] bool isHandleLoaded;
        [SerializeField] int refCount;
        [SerializeField] List<IUsingAddressabledAsset> usingAddressabledAssetList = new List<IUsingAddressabledAsset>();

        public AssetHandle(string assetAddress)
        {
            AssetAddress = assetAddress;
        }

        // 클리어가 작동되면 모든 프리펩을 파괴하고 오브젝트 매니저의 딕셔너리를 초기화 시킴.
        public void AssetHandleClear()
        {
            if (isHandleLoaded)
            {
                Addressables.Release(handle);
                isHandleLoaded = false;
            }
        }

        void LoadAssetHandle()
        {
            if (!isHandleLoaded)
            {
                handle = Addressables.LoadAssetAsync<T>(AssetAddress);
                handle.Completed +=
                    (_handle) =>
                    {
                        isHandleLoaded = true;
                    };
                handle.WaitForCompletion();
            }
        }

        public AssetHandle<T> GetAssetHande(IUsingAddressabledAsset usingAddressabledAsset)
        {
            if (!usingAddressabledAssetList.Contains(usingAddressabledAsset))
            {
                usingAddressabledAssetList.Add(usingAddressabledAsset);
                usingAddressabledAsset.DereferencingAssetHandleAct += DereferencingAssetHandle;

                refCount++;
            }

            return this;
        }

        public T GetAsset()
        {
            LoadAssetHandle();

            return handle.Result;
        }

        public Sprite GetAtlasedSprite(string spriteName)
        {
            LoadAssetHandle();

            return (handle.Result as SpriteAtlas).GetSprite(spriteName);
        }

        public void DereferencingAssetHandle(IUsingAddressabledAsset usingAddressabledAsset)
        {
            if (usingAddressabledAssetList.Contains(usingAddressabledAsset))
            {
                usingAddressabledAssetList.Remove(usingAddressabledAsset);
                usingAddressabledAsset.DereferencingAssetHandleAct -= DereferencingAssetHandle;

                refCount--;
            }

            if (refCount == 0)
                AssetHandleClear();
        }
    }
}