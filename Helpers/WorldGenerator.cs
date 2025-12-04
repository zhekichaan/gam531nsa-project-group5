using FinalProject.Common;
using OpenTK.Mathematics;

namespace FinalProject.Helpers
{
    public class WorldGenerator
    {
        private readonly Random _random;

        public WorldGenerator(int seed = 42)
        {
            _random = new Random(seed);
        }

        // Generate trees and bushes across the world
        public void GenerateForest(
            List<WorldObject> worldObjects,
            List<Mesh> treeMeshes,
            List<Mesh> bushMeshes,
            List<Vector3> exclusionZones,
            float exclusionRadius = 15f,
            float worldSize = 60f)
        {
            if (treeMeshes.Count == 0)
            {
                Console.WriteLine("[WORLD GEN] No tree meshes provided!");
                return;
            }

            Console.WriteLine("[WORLD GEN] Starting forest generation...");

            // Spawn trees
            int treesSpawned = 0;
            for (int i = 0; i < 600; i++) 
            {
                if (treesSpawned >= 550) break;

                float x = (float)(_random.NextDouble() * worldSize * 2 - worldSize);
                float z = (float)(_random.NextDouble() * worldSize * 2 - worldSize);
                Vector3 pos = new Vector3(x, -0.8f, z);

                // Check exclusion zones
                bool tooClose = false;
                foreach (var zone in exclusionZones)
                {
                    if (Vector3.Distance(pos, zone) < exclusionRadius)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                // Pick random tree
                Mesh randomTree = treeMeshes[_random.Next(treeMeshes.Count)];
                float scale = 0.008f + (float)(_random.NextDouble() * 0.004f);
                float rotation = (float)(_random.NextDouble() * Math.PI * 2);

                worldObjects.Add(new WorldObject(
                    randomTree,
                    pos,
                    new Vector3(scale),
                    rotation,
                    true,
                    new Vector3(0.5f, 10f, 0.5f)
                ));

                treesSpawned++;
            }

            Console.WriteLine($"[WORLD GEN] Spawned {treesSpawned} trees");

            // Spawn bushes
            if (bushMeshes.Count > 0)
            {
                int bushesSpawned = 0;
                for (int i = 0; i < 500; i++)
                {
                    if (bushesSpawned >= 450) break;

                    float x = (float)(_random.NextDouble() * worldSize * 2 - worldSize);
                    float z = (float)(_random.NextDouble() * worldSize * 2 - worldSize);
                    Vector3 pos = new Vector3(x, -0.5f, z);

                    bool tooClose = false;
                    foreach (var zone in exclusionZones)
                    {
                        if (Vector3.Distance(pos, zone) < exclusionRadius)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                    if (tooClose) continue;

                    Mesh randomBush = bushMeshes[_random.Next(bushMeshes.Count)];
                    float scale = 0.003f + (float)(_random.NextDouble() * 0.002f);
                    float rotation = (float)(_random.NextDouble() * Math.PI * 2);

                    worldObjects.Add(new WorldObject(
                        randomBush,
                        pos,
                        new Vector3(scale),
                        rotation,
                        false,
                        new Vector3(0.5f, 2f, 0.5f)
                    ));

                    bushesSpawned++;
                }

                Console.WriteLine($"[WORLD GEN] Spawned {bushesSpawned} bushes");
            }
        }

        // Generate collectible batteries in safe locations
        public void GenerateBatteries(
            List<CollectibleBattery> batteries,
            Mesh batteryMesh,
            int count,
            List<Vector3> exclusionZones,
            float exclusionRadius = 6f,
            float worldSize = 50f)
        {
            Console.WriteLine($"[WORLD GEN] Spawning {count} batteries...");

            int spawned = 0;
            int attempts = 0;

            while (spawned < count && attempts < count * 40)
            {
                attempts++;

                float x = (float)(_random.NextDouble() * worldSize * 1.6f - worldSize * 0.8f);
                float z = (float)(_random.NextDouble() * worldSize * 1.6f - worldSize * 0.8f);
                Vector3 pos = new Vector3(x, 0.3f, z);

                // Check exclusion zones
                bool tooClose = false;
                foreach (var zone in exclusionZones)
                {
                    if (Vector3.Distance(pos, zone) < exclusionRadius)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                // Check against other batteries (space them out)
                bool tooCloseToOtherBattery = false;
                foreach (var battery in batteries)
                {
                    if (Vector3.Distance(pos, battery.Position) < 15f)
                    {
                        tooCloseToOtherBattery = true;
                        break;
                    }
                }
                if (tooCloseToOtherBattery) continue;

                float recharge = 30f + (float)(_random.NextDouble() * 20f);
                batteries.Add(new CollectibleBattery(batteryMesh, pos, new Vector3(0.8f), recharge));
                spawned++;
            }

            Console.WriteLine($"[WORLD GEN] Spawned {spawned}/{count} batteries");
        }
    }
}