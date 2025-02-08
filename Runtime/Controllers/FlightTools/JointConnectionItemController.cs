using System;
using System.Collections;
using DebugTools.Runtime.UI.FlightTools;
using KSP.Sim;
using KSP.Sim.impl;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace DebugTools.Runtime.Controllers.FlightTools
{
    internal class
        JointConnectionItemController : ItemController<PartOwnerBehavior.JointConnection, JointConnectionItem>
    {
        private readonly string[] _attachNodeTypeEnumNames = Enum.GetNames(typeof(AttachNodeType));

        private readonly string[] _jointConnectionTypeEnumNames =
            Enum.GetNames(typeof(PartOwnerBehavior.JointConnectionType));

        public JointConnectionItemController()
        {
            Item.Destroy.clicked += DestroyJoint;
        }

        public override void SyncTo(PartOwnerBehavior.JointConnection? jointConnection)
        {
            if (jointConnection == null)
            {
                Item.NumJoints.text = "N/A";
                Item.HostName.text = "N/A";
                Item.TargetName.text = "N/A";
                Item.AttachmentType.text = "N/A";
                Item.JointType.text = "N/A";
            }
            else
            {
                Item.NumJoints.text = jointConnection.connectionType == PartOwnerBehavior.JointConnectionType.Physical
                    ? GetNumJoints(jointConnection).ToString()
                    : "N/A";

                Item.HostName.text = jointConnection?.host.Name ?? "NULL";
                Item.TargetName.text = jointConnection?.target.Name ?? "NULL";

                Item.AttachmentType.text = _attachNodeTypeEnumNames[(int)jointConnection!.nodeType];
                Item.JointType.text = _jointConnectionTypeEnumNames[(int)jointConnection.connectionType];
            }

            Model = jointConnection;
        }

        private static int GetNumJoints(PartOwnerBehavior.JointConnection jointConnection)
        {
            if (jointConnection.connectionType != PartOwnerBehavior.JointConnectionType.Physical) return 0;

            var num = 0;
            if (jointConnection?.Joints != null)
                foreach (var joint in jointConnection.Joints)
                    if (joint != null && joint.connectedBody != null)
                        num++;

            return num;
        }

        private void DestroyJoint()
        {
            if (Model == null)
                return;

            var host = Model.host;
            if (host == null) return;

            var partOwner = host.GetPartOwner();
            if (partOwner != null)
            {
                CoroutineUtil.Instance.StartCoroutine(CoroutineDestroyJoint(partOwner, Model));
            }
        }

        private IEnumerator CoroutineDestroyJoint(PartOwnerBehavior hostPartOwner,
            PartOwnerBehavior.JointConnection jointConnection)
        {
            yield return new WaitForFixedUpdate();
            
            DebugToolsPlugin.Logger.LogInfo(
                "<color=green> JOINT BROKE(MANUAL): </color>" +
                $"Owner: {Model?.host.Name}, Connected to: {Model?.target.Name}");
            
            hostPartOwner.BreakJointConnection(jointConnection);
            DestroyButtonClicked?.Invoke();
        }
    }
}