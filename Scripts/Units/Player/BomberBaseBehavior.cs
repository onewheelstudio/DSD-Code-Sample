using HexGame.Grid;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HexGame.Units
{
    public class BomberBaseBehavior : UnitBehavior, IWaitForTarget
    {
        private CameraControlActions cameraControls;
        private InputAction leftClick;
        private InputAction rightClick;
        private UnitStorageBehavior storageBehavior;
        private BomberMoveBehavior[] bombers;

        [SerializeField] private Hex3 targetLocation;

        public override void StartBehavior()
        {
            _isFunctional = true;
            bombers = this.GetComponentsInChildren<BomberMoveBehavior>();
            storageBehavior.CheckResourceLevels();
        }

        public override void StopBehavior()
        {
            _isFunctional = false;
        }

        private void Awake()
        {
            cameraControls = new CameraControlActions();
            leftClick = cameraControls.PointerInput.LeftClick;
            rightClick = cameraControls.PointerInput.RightClick;

            storageBehavior = this.GetComponent<UnitStorageBehavior>();
        }

        private void OnEnable()
        {
            leftClick.performed += SetTarget;
            rightClick.performed += CancelTarget;
            DayNightManager.toggleDay += RequestResources;
        }

        private void OnDisable()
        {
            leftClick.performed -= SetTarget;
            rightClick.performed -= CancelTarget;
            DayNightManager.toggleDay -= RequestResources;
        }

        private void RequestResources(int obj)
        {
            storageBehavior.CheckResourceLevels();
        }

        public void StartListeningForTarget()
        {
            Debug.Log("Waiting for Target");
            leftClick.Enable();
            rightClick.Enable();
        }

        private void SetTarget(InputAction.CallbackContext obj)
        {
            if (storageBehavior.efficiency <= 0.01f)
            {
                MessagePanel.ShowMessage("No workers.", this.gameObject);
                CancelTarget(obj);
                return;
            }

            this.targetLocation = HelperFunctions.GetMouseVector3OnPlane(true);

            if (Hex3.DistanceBetween(this.transform.position, this.targetLocation) <= GetStat(Stat.minRange))
            {
                MessagePanel.ShowMessage("Inside Minimum Distance", this.gameObject);
                return;
            }
            else if (Hex3.DistanceBetween(this.transform.position, this.targetLocation) > GetStat(Stat.maxRange))
            {
                MessagePanel.ShowMessage("Outside Maximum Distance", this.gameObject);
                return;
            }

            foreach (var bomber in bombers)
            {
                bomber.SetDestination(this.targetLocation);
            }

            StartCoroutine(DoLaunch(this.targetLocation));
            CancelTarget(obj);
        }

        private void CancelTarget(InputAction.CallbackContext obj)
        {
            Debug.Log("Cancel -> Waiting for Target");
            leftClick.Disable();
            rightClick.Disable();

            GameObject.FindObjectOfType<CursorManager>().SetCursor(CursorType.hex);
        }

        IEnumerator DoLaunch(Hex3 location)
        {
            yield return null;
        }
    }
}
