/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Leap;
using Leap.Unity.Attributes;
using UnityEngine.Serialization;

namespace Leap.Unity {

  /// <summary> A skinned and jointed 3D HandModel. </summary>
  public class RiggedHand : HandModel {

    public override ModelType HandModelType { get { return ModelType.Graphics; }}
    public override bool SupportsEditorPersistence() { return true; }

    [SerializeField]
    [FormerlySerializedAs("DeformPositionsInFingers")]
    [OnEditorChange("deformPositionsInFingers")]
    private bool _deformPositionsInFingers = true;
    public bool deformPositionsInFingers {
      get { return _deformPositionsInFingers; }
      set {
        _deformPositionsInFingers = value;
        updateDeformPositionsInFingers();
      }
    }

    [Tooltip("Because bones only exist at their roots in model rigs, the length " +
      "of the last fingertip bone is lost when placing bones at positions in the " +
      "tracked hand. " +
      "This option scales the last bone along its X axis (length axis) to match " +
      "its bone length to the tracked bone length. This option only has an " +
      "effect if Deform Positions In Fingers is enabled.")]
    [DisableIf("_deformPositionsInFingers", isEqualTo: false)]
    [SerializeField]
    [OnEditorChange("scaleLastFingerBones")]
    private bool _scaleLastFingerBones = true;
    public bool scaleLastFingerBones {
      get { return _scaleLastFingerBones; }
      set {
        _scaleLastFingerBones = value;
        updateScaleLastFingerBoneInFingers();
      }
    }
    
    [Tooltip("Hands are typically rigged in 3D packages with the palm transform near the wrist. Uncheck this if your model's palm transform is at the center of the palm similar to Leap API hands.")]
    [FormerlySerializedAs("ModelPalmAtLeapWrist")]
    public bool modelPalmAtLeapWrist = true;

    [Tooltip("Set to True if each finger has an extra trasform between palm and base of the finger.")]
    [FormerlySerializedAs("UseMetaCarpals")]
    public bool useMetaCarpals;

    #pragma warning disable 0414
    // TODO: DELETEME these
    [Header("Values for Stored Start Pose")]
    [SerializeField]
    private List<Transform> jointList = new List<Transform>();
    [SerializeField]
    private List<Quaternion> localRotations = new List<Quaternion>();
    [SerializeField]
    private List<Vector3> localPositions = new List<Vector3>();
    #pragma warning restore 0414

    [Tooltip("If non-zero, this vector and the modelPalmFacing vector " +
      "will be used to re-orient the Transform bones in the hand rig, to " +
      "compensate for bone axis discrepancies between Leap Bones and model " +
      "bones.")]
    public Vector3 modelFingerPointing = new Vector3(0, 0, 0);
    [Tooltip("If non-zero, this vector and the modelFingerPointing vector " +
      "will be used to re-orient the Transform bones in the hand rig, to " +
      "compensate for bone axis discrepancies between Leap Bones and model " +
      "bones.")]
    public Vector3 modelPalmFacing = new Vector3(0, 0, 0);

    /// <summary> Rotation derived from the `modelFingerPointing` and
    /// `modelPalmFacing` vectors in the RiggedHand inspector. </summary>
    public Quaternion userBoneRotation { get {
      if (modelFingerPointing == Vector3.zero || modelPalmFacing == Vector3.zero) {
        return Quaternion.identity;
      }
      return Quaternion.Inverse(
        Quaternion.LookRotation(modelFingerPointing, -modelPalmFacing));
    }}

    public override void InitHand() {
      UpdateHand();
      updateDeformPositionsInFingers();
      updateScaleLastFingerBoneInFingers();
    }

    public override void UpdateHand() {
      if (palm != null) {
        if (modelPalmAtLeapWrist) {
          palm.position = GetWristPosition();
        }
        else {
          palm.position = GetPalmPosition();
          if (wristJoint) {
            wristJoint.position = GetWristPosition();
          }
        }
        palm.rotation = getRiggedPalmRotation() * userBoneRotation;
      }

      if (forearm != null) {
        forearm.rotation = GetArmRotation() * userBoneRotation;
      }

      for (int i = 0; i < fingers.Length; ++i) {
        if (fingers[i] != null) {
          fingers[i].fingerType = (Finger.FingerType)i;
          fingers[i].UpdateFinger();
        }
      }
    }

    /**Sets up the rigged hand by finding base of each finger by name */
    [ContextMenu("Setup Rigged Hand")]
    public void SetupRiggedHand() {
      Debug.Log("Using transform naming to setup RiggedHand on " + transform.name);
      modelFingerPointing = new Vector3(0, 0, 0);
      modelPalmFacing = new Vector3(0, 0, 0);
      assignRiggedFingersByName();
      setupRiggedFingers();
      modelPalmFacing = calculateModelPalmFacing(palm, fingers[2].transform, fingers[1].transform);
      modelFingerPointing = calculateModelFingerPointing();
      setFingerPalmFacing();
    }
    
    /**Sets up the rigged hand if RiggedFinger scripts have already been assigned using Mecanim values.*/
    public void AutoRigRiggedHand(Transform palm, Transform finger1, Transform finger2) {
      Debug.Log("Using Mecanim mapping to setup RiggedHand on " + transform.name);
      modelFingerPointing = new Vector3(0, 0, 0);
      modelPalmFacing = new Vector3(0, 0, 0);
      setupRiggedFingers();
      modelPalmFacing = calculateModelPalmFacing(palm, finger1, finger2);
      modelFingerPointing = calculateModelFingerPointing();
      setFingerPalmFacing();
    }

    /**Finds palm and finds root of each finger by name and assigns RiggedFinger scripts */
    private void assignRiggedFingersByName(){
      List<string> palmStrings = new List<string> { "palm"};
      List<string> thumbStrings = new List<string> { "thumb", "tmb" };
      List<string> indexStrings = new List<string> { "index", "idx"};
      List<string> middleStrings = new List<string> { "middle", "mid"};
      List<string> ringStrings = new List<string> { "ring"};
      List<string> pinkyStrings = new List<string> { "pinky", "pin"};
      //find palm by name
      //Transform palm = null;
      Transform thumb = null;
      Transform index = null;
      Transform middle = null;
      Transform ring = null;
      Transform pinky = null;
      Transform[] children = transform.GetComponentsInChildren<Transform>();
      if (palmStrings.Any(w => transform.name.ToLower().Contains(w))){
        base.palm = transform;
      }
      else{
        foreach (Transform t in children) {
          if (palmStrings.Any(w => t.name.ToLower().Contains(w)) == true) {
            base.palm = t;

          }
        }
      }
      if (!palm) {
        palm = transform;
      }
      if (palm) {
        foreach (Transform t in children) {
          RiggedFinger preExistingRiggedFinger;
          preExistingRiggedFinger = t.GetComponent<RiggedFinger>();
          string lowercaseName = t.name.ToLower();
          if (!preExistingRiggedFinger) {
            if (thumbStrings.Any(w => lowercaseName.Contains(w)) && t.parent == palm) {
              thumb = t;
              RiggedFinger newRiggedFinger = thumb.gameObject.AddComponent<RiggedFinger>();
              newRiggedFinger.fingerType = Finger.FingerType.TYPE_THUMB;
            }
            if (indexStrings.Any(w => lowercaseName.Contains(w)) && t.parent == palm) {
              index = t;
              RiggedFinger newRiggedFinger = index.gameObject.AddComponent<RiggedFinger>();
              newRiggedFinger.fingerType = Finger.FingerType.TYPE_INDEX;
            }
            if (middleStrings.Any(w => lowercaseName.Contains(w)) && t.parent == palm) {
              middle = t;
              RiggedFinger newRiggedFinger = middle.gameObject.AddComponent<RiggedFinger>();
              newRiggedFinger.fingerType = Finger.FingerType.TYPE_MIDDLE;
            }
            if (ringStrings.Any(w => lowercaseName.Contains(w)) && t.parent == palm) {
              ring = t;
              RiggedFinger newRiggedFinger = ring.gameObject.AddComponent<RiggedFinger>();
              newRiggedFinger.fingerType = Finger.FingerType.TYPE_RING;
            }
            if (pinkyStrings.Any(w => lowercaseName.Contains(w)) && t.parent == palm) {
              pinky = t;
              RiggedFinger newRiggedFinger = pinky.gameObject.AddComponent<RiggedFinger>();
              newRiggedFinger.fingerType = Finger.FingerType.TYPE_PINKY;
            }
          }
        }
      }
    }

    /**Triggers SetupRiggedFinger() in each RiggedFinger script for this RiggedHand */
    private void setupRiggedFingers() {
      RiggedFinger[] fingerModelList = GetComponentsInChildren<RiggedFinger>();
      for (int i = 0; i < 5; i++) {
        int fingersIndex = fingerModelList[i].fingerType.indexOf();
        fingers[fingersIndex] = fingerModelList[i];
        fingerModelList[i].SetupRiggedFinger(useMetaCarpals);
      }
    }

    /**Sets the modelPalmFacing vector in each RiggedFinger to match this RiggedHand */
    private void setFingerPalmFacing() {
      RiggedFinger[] fingerModelList = GetComponentsInChildren<RiggedFinger>();
      for (int i = 0; i < 5; i++) {
        int fingersIndex = fingerModelList[i].fingerType.indexOf();
        fingers[fingersIndex] = fingerModelList[i];
        fingerModelList[i].modelPalmFacing = modelPalmFacing;
      }
    }

    /**Calculates the palm facing direction by finding the vector perpendicular to the palm and two fingers  */
    private Vector3 calculateModelPalmFacing(Transform palm, Transform finger1,
      Transform finger2)
    {
      Vector3 a = palm.transform.InverseTransformPoint(palm.position);
      Vector3 b = palm.transform.InverseTransformPoint(finger1.position);
      Vector3 c = palm.transform.InverseTransformPoint(finger2.position);

      Vector3 side1 = b - a;
      Vector3 side2 = c - a;
      Vector3 perpendicular;

      if (Handedness == Chirality.Left) {
        perpendicular = Vector3.Cross(side1, side2);
      }
      else perpendicular = Vector3.Cross(side2, side1);
      //flip perpendicular if it is above palm
      Vector3 calculatedPalmFacing = CalculateZeroedVector(perpendicular);
      return calculatedPalmFacing;
    }

    /**Find finger direction by finding distance vector from palm to middle finger */
    private Vector3 calculateModelFingerPointing() {
      Vector3 distance = palm.transform.InverseTransformPoint(fingers[2].transform.GetChild(0).transform.position) - palm.transform.InverseTransformPoint(palm.position);
      Vector3 calculatedFingerPointing = CalculateZeroedVector(distance);
      return calculatedFingerPointing * -1f;
    }

    /**Finds nearest cardinal vector to a vector */
    public static Vector3 CalculateZeroedVector(Vector3 vectorToZero) {
      var zeroed = new Vector3();
      float max = Mathf.Max(Mathf.Abs(vectorToZero.x), Mathf.Abs(vectorToZero.y), Mathf.Abs(vectorToZero.z));
      if (Mathf.Abs(vectorToZero.x) == max) {
        zeroed = (vectorToZero.x < 0) ? new Vector3(1, 0, 0) : new Vector3(-1, 0, 0);
      }
      if (Mathf.Abs(vectorToZero.y) == max) {
        zeroed = (vectorToZero.y < 0) ? new Vector3(0, 1, 0) : new Vector3(0, -1, 0);
      }
      if (Mathf.Abs(vectorToZero.z) == max) {
        zeroed = (vectorToZero.z < 0) ? new Vector3(0, 0, 1) : new Vector3(0, 0, -1);
      }
      return zeroed;
    }

    // /**Stores a snapshot of original joint positions */
    // [ContextMenu("StoreJointsStartPose")]
    // public void StoreJointsStartPose() {
    //   foreach (Transform t in palm.parent.GetComponentsInChildren<Transform>()) {
    //     jointList.Add(t);
    //     localRotations.Add(t.localRotation);
    //     localPositions.Add(t.localPosition);
    //   }
    // }

    // /**Restores original joint positions, particularly after model has been placed in Leap's editor pose */
    // [ContextMenu("RestoreJointsStartPose")]
    // public void RestoreJointsStartPose() {
    //   //Debug.Log("RestoreJointsStartPose()");
    //   for (int i = 0; i < jointList.Count; i++) {
    //     Transform jointTrans = jointList[i];
    //     jointTrans.localRotation = localRotations[i];
    //     jointTrans.localPosition = localPositions[i];
    //   }
    // }

    private void updateDeformPositionsInFingers() {
      var riggedFingers = GetComponentsInChildren<RiggedFinger>();
      foreach (var finger in riggedFingers){
        finger.deformPosition = deformPositionsInFingers;
      }
    }

    private void updateScaleLastFingerBoneInFingers() {
      var riggedFingers = GetComponentsInChildren<RiggedFinger>();
      foreach (var finger in riggedFingers) {
        finger.scaleLastFingerBone = scaleLastFingerBones;
      }
    }

    // These versions of GetPalmRotation & CalculateRotation return the opposite
    // vector compared to LeapUnityExtension.CalculateRotation.
    // This will be deprecated once LeapUnityExtension.CalculateRotation is
    // flipped in the next release of LeapMotion Core Assets.
    private Quaternion getRiggedPalmRotation() {
      if (hand_ != null) {
        LeapTransform trs = hand_.Basis;
        return calculateRotation(trs);
      }
      if (palm) {
        return palm.rotation;
      }
      return Quaternion.identity;
    }

    private Quaternion calculateRotation(LeapTransform trs) {
      Vector3 up = trs.yBasis.ToVector3();
      Vector3 forward = trs.zBasis.ToVector3();
      return Quaternion.LookRotation(forward, up);
    }

    // [Tooltip("When true, hands will be put into a Leap editor pose near the LeapServiceProvider's transform.  When False, the hands will be returned to their Start Pose if it has been saved.")]
    // [SerializeField]
    // private bool setEditorLeapPose = true;
    
    // public bool SetEditorLeapPose {
    //   get { return setEditorLeapPose; }
    //   set {
    //     if (value == false) {
    //       RestoreJointsStartPose();
    //     }
    //     setEditorLeapPose = value;
    //   }
    // }

    // [Tooltip("When True, hands will be put into a Leap editor pose near the LeapServiceProvider's transform.  When False, the hands will be returned to their Start Pose if it has been saved.")]
    // [SerializeField]
    // [HideInInspector]
    // private bool deformPositionsState = false;

  }

}
