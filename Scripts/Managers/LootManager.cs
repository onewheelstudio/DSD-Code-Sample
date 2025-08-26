using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LootManager : MonoBehaviour
{
    [InfoBox("Define which particles system should be used for each loot spawn and the count for each particle system.")]
    [SerializeField] private ParticleData particleData;
    public static event Action<LootData> lootAdded;
    public static event Action AllLootCollected;
    private Queue<LootData> lootQueue = new();
    private List<LootData> lootData = new();
    private int lootMax = 100;
    public int LootMax => lootMax;

    private Vector3[] anchorPositions;
    [SerializeField, Range(0,10)] private float speed = 1f;
    [SerializeField, Range(0,180)] private float minAngle = 20f;
    [SerializeField, Range(0,180)] private float maxAngle = 60f;
    private ParticleSystem.Particle[] particlesInfo;
    private System.Random random;
    [SerializeField] private float collectedDistance = 0.2f;
    private EnemyCrystalManager ecm;

    private static bool lootCollected = false;
    public static bool LootCollected => lootCollected;

    private void Awake()
    {
        lootMax = GetLootMax();
        random = new System.Random(Time.frameCount);
        ecm = FindFirstObjectByType<EnemyCrystalManager>();
    }

    private void OnEnable()
    {
        lootCollected = true;
        EnemyLootDrop.requestLootDrop += AddLoot;
        CollectionBehavior.collected += HideLoot;
        DayNightManager.transitionToNight += MoveParticles;
    }


    private void OnDisable()
    {
        EnemyLootDrop.requestLootDrop -= AddLoot;
        CollectionBehavior.collected -= HideLoot;
        DayNightManager.transitionToNight -= MoveParticles;
    }
    private void MoveParticles(int dayNumber, float transitionTime)
    {
        anchorPositions = ecm.GetCrystalPositions();
        MoveParticles();
    }

    [Button]
    private async void MoveParticles()
    {
        if (lootData.Count == 0 || anchorPositions.Length == 0)
        {
            Debug.Log("No loot");
            return;
        }

        float time = Time.timeSinceLevelLoad;
        int count = 0;
        int particlesToCollect = lootData.Count(l => !l.isCollected);

        //time check to ensure that enemy units spawn
        float startTime = Time.timeSinceLevelLoad;
        float timeLimit = 30f;

        while (this.enabled)
        {
            count = Mathf.RoundToInt((Time.timeSinceLevelLoad - time) * (Time.timeSinceLevelLoad - time) * 5);

            int particleCount = particleData.particleSystem.particleCount;
            if(particlesInfo == null || particleCount != particlesInfo.Length)
                particlesInfo = new ParticleSystem.Particle[particleCount];
            particleData.particleSystem.GetParticles(particlesInfo);

            //must be called on the main thread
            float deltaTime = Time.deltaTime;
            float currentTime = Time.timeSinceLevelLoad;

            await Awaitable.BackgroundThreadAsync();
            if (lootQueue.Count >= particlesToCollect || currentTime > startTime + timeLimit)
            {
                await Awaitable.MainThreadAsync();
                KillAllParticles();
                AllLootCollected?.Invoke();
                lootCollected = true;
                return;
            }

            for (int k = 0; k < particleCount; k++)
            {
                if(k >= lootData.Count)
                    break;

                LootData data = lootData[k];
                if (data.isCollected)
                    continue;
                if (count < k)
                    continue;

                if (data.anchorPosition == Vector3.zero)
                    data.anchorPosition = GetAnchorPosition(data.position);

                float distance = Vector3.Distance(data.anchorPosition, data.position);
                if (distance <= collectedDistance && !data.isCollected)
                {
                    data.isCollected = true;
                    lootQueue.Enqueue(data);
                    continue;
                }

                float angle = distance > 10 ? minAngle : Mathf.Lerp(minAngle, maxAngle, (10 - distance) / 10);
                Vector3 velocity = Quaternion.Euler(0f, angle + data.jitter, 0f) * (data.anchorPosition - data.position);
                data.position += deltaTime * speed * velocity.normalized;
                particlesInfo[data.particleIndex].position = data.position;
            }
            await Awaitable.MainThreadAsync();
            if(particleData != null && particleData.particleSystem != null)
                particleData.particleSystem.SetParticles(particlesInfo, particleCount);
        }
    }

    private Vector3 GetAnchorPosition(Vector3 position)
    {
        if(anchorPositions.Length == 1)
            return anchorPositions[0];

        float distance = Mathf.Infinity;
        Vector3 anchorPosition = anchorPositions[0];
        for (int i = 0; i < anchorPositions.Length; i++)
        {
            float tempDistance = Vector3.Distance(position, anchorPositions[i]);
            if (tempDistance < distance)
            {
                distance = tempDistance;
                anchorPosition = anchorPositions[i];
            }
        }

        return anchorPosition + Vector3.up * 0.5f;
    }

    private void KillAllParticles()
    {
        ParticleSystem.Particle[] particles;
        int particleCount = particleData.particleSystem.particleCount;
        particles = new ParticleSystem.Particle[particleCount];
        particleData.particleSystem.GetParticles(particles);

        for (int i = 0; i < particleCount; i++)
        {
            particles[i].remainingLifetime = 10000;
            particles[i].position = Vector3.up * 200;
        }

        particleData.particleSystem.SetParticles(particles, particleCount);
        lootData.Clear();
    }

    public void AddLoot(Vector3 position)
    {
        lootCollected = false;
        //if we're at the max loot and there's nothing in the queue, don't add more
        if (lootData.Count >= lootMax && lootQueue.Count == 0)
            return;

        if (lootQueue.Count > 0)
            ReuseParticle(position);
        else if(particleData.particleSystem.main.maxParticles > particleData.particleSystem.particleCount)
            EmitNewParticle(position);
    }

    private void ReuseParticle(Vector3 position)
    {
        LootData data = lootQueue.Dequeue();
        if(data == null)
            return;
        data.position = position;
        data.position.y = 0.1f;
        data.isCollected = false;
        data.anchorPosition = Vector3.zero;

        lootAdded?.Invoke(data);
        lootData.Add(data);
        ParticleSystem.Particle[] particles;

        //get the particles information
        int particleCount = particleData.particleSystem.particleCount;
        particles = new ParticleSystem.Particle[particleCount];
        particleData.particleSystem.GetParticles(particles);

        particles[data.particleIndex].position = data.position;

        //update the particle data - this is the step that finally moves the particles
        particleData.particleSystem.SetParticles(particles, particleCount);
    }

    private void EmitNewParticle(Vector3 position)
    {
        LootData data = new();
        data.position = position;
        data.position.y = 0.1f; 
        data.jitter = random.Next(-5, 5);
        data.anchorPosition = Vector3.zero;

        //track what we're going to spawn
        int startCount = particleData.particleSystem.particleCount;
        data.particleIndex = startCount;

        //spawn the particles based on the data
        particleData.transform.position = data.position;
        particleData.particleSystem.Emit(1);

        lootAdded?.Invoke(data);
        lootData.Add(data);
    }

    private LootData GetLootData()
    {
        return lootQueue.Count > 0 ? lootQueue.Dequeue() : new LootData();
    }

    public void HideLoot(LootData loot)
    {
        ParticleSystem.Particle[] particles;
        loot.isCollected = true;
        lootQueue.Enqueue(loot);

        //get the particles information
        int particleCount = particleData.particleSystem.particleCount;
        particles = new ParticleSystem.Particle[particleCount];
        particleData.particleSystem.GetParticles(particles);

        loot.position += Vector3.up * 100;
        particles[loot.particleIndex].position = loot.position;

        //update the particle data - this is the step that finally moves the particles
        particleData.particleSystem.SetParticles(particles, particleCount);
    }

    public List<LootData> GetNearbyLoot(Vector3 position, float range)
    {
        List<LootData> nearbyLoot = new();
        foreach (LootData loot in lootData)
        {
            if (HelperFunctions.HexRangeFloat(loot.position, position) <= range)
            {
                nearbyLoot.Add(loot);
            }
        }
        return nearbyLoot;
    }

    private int GetLootMax()
    {
        int max = int.MaxValue;

        int tempMax = particleData.particleSystem.main.maxParticles / particleData.particleCount;
        if (tempMax < max)
        {
            max = tempMax;
        }

        return max;
    }

    /// <summary>
    /// Used to store particle system data for each loot spawn
    /// </summary>
    [System.Serializable]
    private class ParticleData
    {
        [OnValueChanged("GetTransform")]
        public ParticleSystem particleSystem;
        [HideInInspector]
        public Transform transform;
        public int particleCount = 1;

        private void GetTransform()
        {
            transform = particleSystem.transform;
        }
    }

    [System.Serializable]
    public class LootData
    {
        public Vector3 position;
        public Vector3 velocity = Vector3.zero;
        /// <summary>
        /// X is the start index, Y is the count
        /// </summary>
        public int particleIndex = -1;
        public Vector3 anchorPosition;
        public bool isCollected = false;
        public int jitter = 0;
    }


}
