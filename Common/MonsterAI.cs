using FinalProject.Common;
using OpenTK.Mathematics;
using System;

namespace FinalProject
{
    public enum MonsterState
    {
        Patrol,
        Attracted,
        Hunting,
        Chasing
    }

    public class MonsterAI
    {
        private WorldObject _monsterObject;
        private MonsterState _currentState;

        // Movement speeds (adjustable)
        public float PatrolSpeed = 0.8f;
        public float AttractedSpeed = 1.5f;
        public float HuntingSpeed = 1.2f;
        public float ChaseSpeed = 3.5f;

        // Rotation speed
        private const float ROTATION_SPEED = 180f;

        // Detection ranges
        private const float LIGHT_DETECTION_RANGE = 40f;
        private const float VISUAL_DETECTION_RANGE = 15f;
        private const float VISUAL_DETECTION_ANGLE = 60f;


        // Patrol variables
        private Vector3 _patrolTarget;
        private float _patrolWaitTimer;
        private const float PATROL_WAIT_TIME = 2f;
        private Random _random;

        // Hunting variables
        private Vector3 _lastKnownPlayerPosition;
        private float _huntingGiveUpTimer;
        private const float HUNTING_TIMEOUT = 8f;

        // Chasing variables
        private float _losePlayerTimer;
        private const float LOSE_PLAYER_TIME = 3f;
        private Vector3 _lastSeenPlayerPosition;

        // Flashlight tracking
        private bool _wasFlashlightOn;

        private Random random;

        public MonsterState CurrentState => _currentState;

        public MonsterAI(WorldObject monsterObject)
        {
            _monsterObject = monsterObject;
            _currentState = MonsterState.Patrol;
            _random = new Random();
            _patrolTarget = GenerateRandomPatrolPoint();
            _wasFlashlightOn = false;
            random = new Random();
        }

        public void Update(float deltaTime, Vector3 playerPosition, bool flashlightOn, AudioComponent growl)
        {
            Vector3 monsterPosition = _monsterObject.Position;
            float distanceToPlayer = Vector3.Distance(monsterPosition, playerPosition);

            double rng = random.NextDouble();

            HandleStateTransitions(playerPosition, flashlightOn, distanceToPlayer);


            switch (_currentState)
            {
                case MonsterState.Patrol:
                    UpdatePatrol(deltaTime);
                    break;

                case MonsterState.Attracted:
                    UpdateAttracted(deltaTime, playerPosition);
                    break;

                case MonsterState.Hunting:
                    UpdateHunting(deltaTime);
                    break;

                case MonsterState.Chasing:
                    if(!growl.IsPlaying)
                    {
                        growl.Play();
                    }
                    UpdateChasing(deltaTime, playerPosition);
                    break;
            }

            _wasFlashlightOn = flashlightOn;
        }

        private void HandleStateTransitions(Vector3 playerPosition, bool flashlightOn, float distanceToPlayer)
        {
            Vector3 monsterPosition = _monsterObject.Position;

            switch (_currentState)
            {
                case MonsterState.Patrol:
                    // If flashlight turns on within range, become attracted
                    if (flashlightOn && distanceToPlayer < LIGHT_DETECTION_RANGE)
                    {
                        TransitionToAttracted(playerPosition);
                    }
                    // If player spotted visually, start chasing
                    else if (CanSeePlayer(playerPosition))
                    {
                        TransitionToChasing(playerPosition);
                    }
                    break;

                case MonsterState.Attracted:
                    // If flashlight turns off, start hunting last known position
                    if (!flashlightOn && _wasFlashlightOn)
                    {
                        TransitionToHunting(playerPosition);
                    }
                    // If player spotted visually start chasing
                    else if (CanSeePlayer(playerPosition))
                    {
                        TransitionToChasing(playerPosition);
                    }
                    // If flashlight still on but out of range return to patrol
                    else if (flashlightOn && distanceToPlayer > LIGHT_DETECTION_RANGE * 1.2f)
                    {
                        TransitionToPatrol();
                    }
                    break;

                case MonsterState.Hunting:
                    // If flashlight turns back on, become attracted
                    if (flashlightOn && distanceToPlayer < LIGHT_DETECTION_RANGE)
                    {
                        TransitionToAttracted(playerPosition);
                    }
                    // If player spotted, start chasing
                    else if (CanSeePlayer(playerPosition))
                    {
                        TransitionToChasing(playerPosition);
                    }
                    // Timeout give up and return to patrol
                    else if (_huntingGiveUpTimer <= 0)
                    {
                        TransitionToPatrol();
                    }
                    break;

                case MonsterState.Chasing:
                    // If lost sight of player for too long hunt at the last position
                    if (!CanSeePlayer(playerPosition))
                    {
                        _losePlayerTimer -= 0.016f;
                        if (_losePlayerTimer <= 0)
                        {
                            TransitionToHunting(_lastSeenPlayerPosition);
                        }
                    }
                    else
                    {
                        _losePlayerTimer = LOSE_PLAYER_TIME;
                        _lastSeenPlayerPosition = playerPosition;
                    }
                    break;
            }
        }

        private void UpdatePatrol(float deltaTime)
        {
            Vector3 monsterPosition = _monsterObject.Position;
            Vector3 directionToTarget = _patrolTarget - monsterPosition;
            directionToTarget.Y = 0;
            float distance = directionToTarget.Length;

            if (distance < 1f)
            {
                // Reached patrol point, wait before choosing new one
                _patrolWaitTimer += deltaTime;
                if (_patrolWaitTimer >= PATROL_WAIT_TIME)
                {
                    _patrolTarget = GenerateRandomPatrolPoint();
                    _patrolWaitTimer = 0;
                }
            }
            else
            {
                // Move toward patrol point
                Vector3 direction = Vector3.Normalize(directionToTarget);
                _monsterObject.Position += direction * PatrolSpeed * deltaTime;

                // Smooth rotate to face movement direction
                SmoothRotateTowards(direction, deltaTime);
            }
        }

        private void UpdateAttracted(float deltaTime, Vector3 playerPosition)
        {
            Vector3 monsterPosition = _monsterObject.Position;
            Vector3 directionToPlayer = playerPosition - monsterPosition;
            directionToPlayer.Y = 0;

            float distanceToPlayer = directionToPlayer.Length;

            // Only move if not too close
            if (distanceToPlayer > 1.5f)
            {
                Vector3 direction = Vector3.Normalize(directionToPlayer);
                _monsterObject.Position += direction * AttractedSpeed * deltaTime;

                // Smooth rotate to face player
                SmoothRotateTowards(direction, deltaTime);
            }
            else
            {
                // Just rotate if very close
                Vector3 direction = Vector3.Normalize(directionToPlayer);
                SmoothRotateTowards(direction, deltaTime);
            }
        }

        private void UpdateHunting(float deltaTime)
        {
            Vector3 monsterPosition = _monsterObject.Position;
            Vector3 directionToTarget = _lastKnownPlayerPosition - monsterPosition;
            directionToTarget.Y = 0;
            float distance = directionToTarget.Length;

            _huntingGiveUpTimer -= deltaTime;

            if (distance < 2f)
            {
                _huntingGiveUpTimer -= deltaTime * 2;
            }
            else
            {
                // Move toward last known position
                Vector3 direction = Vector3.Normalize(directionToTarget);
                _monsterObject.Position += direction * HuntingSpeed * deltaTime;
                SmoothRotateTowards(direction, deltaTime);
            }
        }

        private void UpdateChasing(float deltaTime, Vector3 playerPosition)
        {
            Vector3 monsterPosition = _monsterObject.Position;
            Vector3 directionToPlayer = playerPosition - monsterPosition;
            directionToPlayer.Y = 0;

            float distanceToPlayer = directionToPlayer.Length;


            if (distanceToPlayer > 1.0f)
            {
                Vector3 direction = Vector3.Normalize(directionToPlayer);
                _monsterObject.Position += direction * ChaseSpeed * deltaTime;
                SmoothRotateTowards(direction, deltaTime);
            }
            else
            {
                // Just rotate if very close
                Vector3 direction = Vector3.Normalize(directionToPlayer);
                SmoothRotateTowards(direction, deltaTime);
            }
        }

        private bool CanSeePlayer(Vector3 playerPosition)
        {
            Vector3 monsterPosition = _monsterObject.Position;
            Vector3 toPlayer = playerPosition - monsterPosition;

            //toPlayer.Y = 0; // Ignore vertical difference
            float distance = toPlayer.Length;

            // Too far to see
            if (distance > VISUAL_DETECTION_RANGE)
                return false;

            // Check if player is within viewing cone
            Vector3 monsterForward = GetMonsterForward();
            Vector3 directionToPlayer = Vector3.Normalize(toPlayer);

            float dotProduct = Vector3.Dot(monsterForward, directionToPlayer);
            float angle = MathHelper.RadiansToDegrees(MathF.Acos(dotProduct));

            return angle < VISUAL_DETECTION_ANGLE;
        }

        private void SmoothRotateTowards(Vector3 direction, float deltaTime)
        {
            if (direction.LengthSquared < 0.01f)
                return;

            // Target yaw in RADIANS
            float targetYaw = MathF.Atan2(direction.X, direction.Z);

            // fbx models have wrong forward axis so we adjust by 90 degrees
            targetYaw += MathHelper.DegreesToRadians(-90f);

            float currentYaw = _monsterObject.Rotation;

            // Compute shortest angle difference
            float difference = targetYaw - currentYaw;

            while (difference > MathF.PI) difference -= 2f * MathF.PI;
            while (difference < -MathF.PI) difference += 2f * MathF.PI;

            //dont rotate if already facing close enough
            if (MathF.Abs(difference) < 0.05f)
                return;

            // Apply smooth rotation
            float maxRotationThisFrame = MathHelper.DegreesToRadians(ROTATION_SPEED) * deltaTime;

            float rotationStep = MathF.Sign(difference) * MathF.Min(maxRotationThisFrame, MathF.Abs(difference));

            _monsterObject.Rotation = currentYaw + rotationStep;
        }


        private Vector3 GetMonsterForward()
        {
            float yawRadians = _monsterObject.Rotation;

            // supporting the adjustemtn for the fbx model forward axis
            // see line 297
            yawRadians += MathHelper.DegreesToRadians(90f);
            return new Vector3(MathF.Sin(yawRadians), 0, MathF.Cos(yawRadians));
        }

        private Vector3 GenerateRandomPatrolPoint()
        {
            // Generate random point within patrol area
            float x = (float)(_random.NextDouble() * 40 - 20);
            float z = (float)(_random.NextDouble() * 40 - 20);
            return new Vector3(x, 1.35f, z);
        }

        private void TransitionToPatrol()
        {
            _currentState = MonsterState.Patrol;
            _patrolTarget = GenerateRandomPatrolPoint();
            _patrolWaitTimer = 0;
        }

        private void TransitionToAttracted(Vector3 playerPosition)
        {
            _currentState = MonsterState.Attracted;
            _lastKnownPlayerPosition = playerPosition;
        }

        private void TransitionToHunting(Vector3 playerPosition)
        {
            _currentState = MonsterState.Hunting;
            _lastKnownPlayerPosition = playerPosition;
            _huntingGiveUpTimer = HUNTING_TIMEOUT;
        }

        private void TransitionToChasing(Vector3 playerPosition)
        {
            _currentState = MonsterState.Chasing;
            _lastSeenPlayerPosition = playerPosition;
            _losePlayerTimer = LOSE_PLAYER_TIME;
        }
    }
}