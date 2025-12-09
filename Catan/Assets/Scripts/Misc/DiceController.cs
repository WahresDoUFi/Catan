using System;
using System.Collections;
using GamePlay;
using UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using User;

namespace Misc
{
    public class DiceController : NetworkBehaviour
    {
        public static DiceController Instance;
        
        [SerializeField] private Dice[] dice;
        [SerializeField] private float followSpeed;
        [SerializeField] private float stiffness;
        [SerializeField] private float maxThrowForce;
        [SerializeField] private float stableUntilReset;
        
        private bool _dragging;
        private bool _hasThrown;
        private Coroutine _markDiceStableCoroutine;
        private bool _throwFinished;
        
        private void Awake()
        {
            Instance = this;
            HideDice();
        }

        private void FixedUpdate()
        {
            if (!_dragging) return;
            var targetPosition = CameraController.Instance.MouseWorldPosition(transform.position.y);
            targetPosition -= Vector3.right * 5f;
            foreach (var die in dice)
            {
                die.enabled = false;
                die.SetTarget(targetPosition, followSpeed / Time.fixedDeltaTime, stiffness);
                targetPosition += Vector3.right * 10f + Vector3.up;
            }
        }

        private void Update()
        {
            if (CanThrow())
                CameraController.Instance.EnterOverview();
            if (_dragging || _throwFinished) return;
            foreach (var die in dice)
            {
                if (!die.Active || !die.Stable)
                    return;
            }
            _markDiceStableCoroutine = StartCoroutine(MarkDiceStable());
        }

        private IEnumerator MarkDiceStable()
        {
            _throwFinished = true;
            yield return new WaitForSeconds(stableUntilReset);
            GameManager.Instance.MarkDiceStable();
            Reset();
            _throwFinished = false;
        }

        private bool CanThrow()
        {
            if (_hasThrown) return false;
            if (PauseMenu.IsOpen) return false;
            return GameManager.Instance.CanThrowDice();
        }

        public void Reset()
        {
            _hasThrown = false;
            if (CanThrow())
            {
                PrepareThrow();
            }
            else
            {
                HideDice();
            }
        }

        public void BeginDrag()
        {
            if (!CanThrow()) return;
            _dragging = true;
        }
        
        public void ReleaseDice()
        {
            if (!_dragging) return;
            if (!CanThrow()) return;
            _hasThrown = true;
            _dragging = false;
            DiceThrownRpc();
            for (var i = 0; i < dice.Length; i++)
            {
                var die = dice[i];
                ReleaseDiceAtPositionRpc(i, die.Position, die.Rotation, die.Velocity);
            }
        }

        [Rpc(SendTo.Everyone)]
        private void DiceThrownRpc()
        {
            var result = DiceRoll.GetResult(GameManager.Instance.Seed);
            dice[0].SetTargetNumber(result.first);
            dice[1].SetTargetNumber(result.second);
        }

        [Rpc(SendTo.Everyone)]
        private void ReleaseDiceAtPositionRpc(int dieId, Vector3 position, Vector3 rotation, Vector3 force)
        {
            var die = dice[dieId];
            die.SetThrowPosition(position, rotation, force);
            die.Release(maxThrowForce);
        }

        private void HideDice()
        {
            var targetPosition = transform.position + Vector3.up * 100f;
            foreach (var die in dice)
            {
                die.SetPosition(targetPosition);
            }
        }

        private void PrepareThrow()
        {
            var targetPosition = transform.position - Vector3.right * 5f;
            foreach (var die in dice)
            {
                die.SetPosition(targetPosition);
                targetPosition += Vector3.right * 10f + Vector3.up;
            }
        }
    }
}
