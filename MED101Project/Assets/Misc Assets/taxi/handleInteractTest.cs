using UnityEngine;
using System.Collections;

namespace Valve.VR.InteractionSystem.Sample
{
    [RequireComponent(typeof(Interactable))]

    public class handleInteractTest : MonoBehaviour
    {

        private Hand.AttachmentFlags attachmentFlags = Hand.defaultAttachmentFlags & (~Hand.AttachmentFlags.SnapOnAttach) & (~Hand.AttachmentFlags.DetachOthers) & (~Hand.AttachmentFlags.VelocityMovement);

        private Interactable interactable;

        private bool lastHovering = false;

        public Animator AnimatorObject;

        void Awake()
        {
            interactable = this.GetComponent<Interactable>();
        }

        public void SetBool(string BoolName)
        {
            //Debug.Log(BoolName + " : " + AnimatorObject.GetBool(BoolName));
            AnimatorObject.SetBool(BoolName, !AnimatorObject.GetBool(BoolName));
            //Debug.Log(BoolName + " : " + AnimatorObject.GetBool(BoolName));
        }

        private void HandHoverUpdate(Hand hand)
        {
            GrabTypes startingGrabType = hand.GetGrabStarting();
            bool isGrabEnding = hand.IsGrabEnding(this.gameObject);

            if (interactable.attachedToHand == null && startingGrabType != GrabTypes.None)
            {
                // Save our position/rotation so that we can restore it when we detach
                //oldPosition = transform.position;
                //oldRotation = transform.rotation;

                // Call this to continue receiving HandHoverUpdate messages,
                // and prevent the hand from hovering over anything else
                hand.HoverLock(interactable);

                // Attach this object to the hand
                hand.AttachObject(gameObject, startingGrabType, attachmentFlags);
            }
            else if (isGrabEnding)
            {
                // Detach this object from the hand
                hand.DetachObject(gameObject);

                // Call this to undo HoverLock
                hand.HoverUnlock(interactable);

                // Restore position/rotation
                //transform.position = oldPosition;
                //transform.rotation = oldRotation;
            }
        }


        private void OnAttachedToHand(Hand hand)
        {
            //generalText.text = string.Format("Attached: {0}", hand.name);
            //attachTime = Time.time;
            SetBool("DoorOpen");
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        private void Update()
        {
            if (interactable.isHovering != lastHovering) //save on the .tostrings a bit
            {
                //hoveringText.text = string.Format("Hovering: {0}", interactable.isHovering);
                lastHovering = interactable.isHovering;
            }
        }
    }
}
