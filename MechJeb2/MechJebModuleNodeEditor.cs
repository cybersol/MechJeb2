using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleNodeEditor : DisplayModule
    {
        EditableDouble prograde = 0;
        EditableDouble radialPlus = 0;
        EditableDouble normalPlus = 0;
        [Persistent(pass = (int)Pass.Global)]
        EditableDouble progradeDelta = 0;
        [Persistent(pass = (int)Pass.Global)]
        EditableDouble radialPlusDelta = 0;
        [Persistent(pass = (int)Pass.Global)]
        EditableDouble normalPlusDelta = 0;
        [Persistent(pass = (int)Pass.Global)]
        EditableTime timeOffset = 0;
        EditableDouble inputAngleTo = 0;
        [Persistent(pass = (int)Pass.Global)]
        EditableInt retrogradeSelect = 0;

        ManeuverNode node;
        ManeuverGizmo gizmo;

        enum Snap { PERIAPSIS, APOAPSIS, REL_ASCENDING, REL_DESCENDING, EQ_ASCENDING, EQ_DESCENDING };
        static int numSnaps = Enum.GetNames(typeof(Snap)).Length;
        Snap snap = Snap.PERIAPSIS;
        string[] snapStrings = new string[] { "periapsis", "apoapsis", "AN with target", "DN with target", "equatorial AN", "equatorial DN" };
        string[] AngleStrings = new string[] { "prograde", "retrograde" };

        void GizmoUpdateHandler(Vector3d dV, double UT)
        {
            prograde = dV.z;
            radialPlus = dV.x;
            normalPlus = dV.y;
            if (retrogradeSelect == 0)
                inputAngleTo = AngleToProgradeAtUT(UT);
            else
                inputAngleTo = AngleToProgradeAtUT(UT) - 180;
        }

        protected override void WindowGUI(int windowID)
        {
            if (vessel.patchedConicSolver.maneuverNodes.Count == 0)
            {
                GUILayout.Label("No maneuver nodes to edit.");
                RelativityModeSelectUI();
                base.WindowGUI(windowID);
                return;
            }

            GUILayout.BeginVertical();

            ManeuverNode oldNode = node;

            if (vessel.patchedConicSolver.maneuverNodes.Count == 1)
            {
                node = vessel.patchedConicSolver.maneuverNodes[0];
            }
            else
            {
                if (!vessel.patchedConicSolver.maneuverNodes.Contains(node)) node = vessel.patchedConicSolver.maneuverNodes[0];

                int nodeIndex = vessel.patchedConicSolver.maneuverNodes.IndexOf(node);
                int numNodes = vessel.patchedConicSolver.maneuverNodes.Count;

                nodeIndex = GuiUtils.ArrowSelector(nodeIndex, numNodes, "Maneuver node #" + (nodeIndex + 1));

                node = vessel.patchedConicSolver.maneuverNodes[nodeIndex];
            }

            if (node != oldNode)
            {
                prograde = node.DeltaV.z;
                radialPlus = node.DeltaV.x;
                normalPlus = -node.DeltaV.y;
                if (retrogradeSelect == 0)
                    inputAngleTo = AngleToProgradeAtUT(node.UT);
                else
                    inputAngleTo = AngleToProgradeAtUT(node.UT) - 180;
            }

            if (gizmo != node.attachedGizmo)
            {
                if (gizmo != null) gizmo.OnGizmoUpdated -= GizmoUpdateHandler;
                gizmo = node.attachedGizmo;
                if (gizmo != null) gizmo.OnGizmoUpdated += GizmoUpdateHandler;
            }

            GUILayout.BeginHorizontal();
            GuiUtils.SimpleTextBox("Prograde:", prograde, "m/s", 60);
            if (GUILayout.Button("-", GUILayout.ExpandWidth(false)))
            {
                prograde -= progradeDelta;
                node.OnGizmoUpdated(new Vector3d(radialPlus, normalPlus, prograde), node.UT);
            }
            progradeDelta.text = GUILayout.TextField(progradeDelta.text, GUILayout.Width(50));            
            if (GUILayout.Button("+", GUILayout.ExpandWidth(false)))
            {
                prograde += progradeDelta;
                node.OnGizmoUpdated(new Vector3d(radialPlus, normalPlus, prograde), node.UT);
            }
            GUILayout.Label("m/s", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GuiUtils.SimpleTextBox("Radial+:", radialPlus, "m/s", 60);
            if (GUILayout.Button("-", GUILayout.ExpandWidth(false)))
            {
                radialPlus -= radialPlusDelta;
                node.OnGizmoUpdated(new Vector3d(radialPlus, normalPlus, prograde), node.UT);
            }
            radialPlusDelta.text = GUILayout.TextField(radialPlusDelta.text, GUILayout.Width(50));
            if (GUILayout.Button("+", GUILayout.ExpandWidth(false)))
            {
                radialPlus += radialPlusDelta;
                node.OnGizmoUpdated(new Vector3d(radialPlus, normalPlus, prograde), node.UT);
            }
            GUILayout.Label("m/s", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GuiUtils.SimpleTextBox("Normal+:", normalPlus, "m/s", 60);
            if (GUILayout.Button("-", GUILayout.ExpandWidth(false)))
            {
                normalPlus -= normalPlusDelta;
                node.OnGizmoUpdated(new Vector3d(radialPlus, normalPlus, prograde), node.UT);
            }
            normalPlusDelta.text = GUILayout.TextField(normalPlusDelta.text, GUILayout.Width(50));            
            if (GUILayout.Button("+", GUILayout.ExpandWidth(false)))
            {
                normalPlus += normalPlusDelta;
                node.OnGizmoUpdated(new Vector3d(radialPlus, normalPlus, prograde), node.UT);
            }
            GUILayout.Label("m/s", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Update")) node.OnGizmoUpdated(new Vector3d(radialPlus, normalPlus, prograde), node.UT);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Shift time by", GUILayout.ExpandWidth(true)))
            {
                node.OnGizmoUpdated(node.DeltaV, node.UT + timeOffset);
            }
            timeOffset.text = GUILayout.TextField(timeOffset.text, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Snap node to", GUILayout.ExpandWidth(true)))
            {
                Orbit o = node.patch;
                double UT = node.UT;
                switch (snap)
                {
                    case Snap.PERIAPSIS:
                        UT = o.NextPeriapsisTime(UT - o.period / 2); //period is who-knows-what for e > 1, but this should still work
                        break;

                    case Snap.APOAPSIS:
                        if (o.eccentricity < 1) UT = o.NextApoapsisTime(UT - o.period / 2);
                        break;

                    case Snap.EQ_ASCENDING:
                        if (o.AscendingNodeEquatorialExists()) UT = o.TimeOfAscendingNodeEquatorial(UT - o.period / 2);
                        break;

                    case Snap.EQ_DESCENDING:
                        if (o.DescendingNodeEquatorialExists()) UT = o.TimeOfDescendingNodeEquatorial(UT - o.period / 2);
                        break;

                    case Snap.REL_ASCENDING:
                        if (core.target.NormalTargetExists && core.target.Orbit.referenceBody == o.referenceBody)
                        {
                            if (o.AscendingNodeExists(core.target.Orbit)) UT = o.TimeOfAscendingNode(core.target.Orbit, UT - o.period / 2);
                        }
                        break;

                    case Snap.REL_DESCENDING:
                        if (core.target.NormalTargetExists && core.target.Orbit.referenceBody == o.referenceBody)
                        {
                            if (o.DescendingNodeExists(core.target.Orbit)) UT = o.TimeOfDescendingNode(core.target.Orbit, UT - o.period / 2);
                        }
                        break;
                }
                node.OnGizmoUpdated(node.DeltaV, UT);
            }
            snap = (Snap)GuiUtils.ArrowSelector((int)snap, numSnaps, snapStrings[(int)snap]);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            int retrogradeSelect_old = retrogradeSelect;
            retrogradeSelect = GUILayout.Toolbar(retrogradeSelect, AngleStrings);
            if (retrogradeSelect != retrogradeSelect_old)
            {
                if (retrogradeSelect == 0)
                    inputAngleTo = AngleToProgradeAtUT(node.UT);
                else
                    inputAngleTo = AngleToProgradeAtUT(node.UT) - 180;
            }
            inputAngleTo.text = GUILayout.TextField(inputAngleTo.text, GUILayout.Width(60));
            GUILayout.Label("º", GUILayout.ExpandWidth(false));
            if (GUILayout.Button("Set", GUILayout.ExpandWidth(true)))
            {
                node.OnGizmoUpdated(node.DeltaV, progradeAngleChangeUT());
                // The calculation is not so accurate, and doing again seems to help massively
                node.OnGizmoUpdated(node.DeltaV, progradeAngleChangeUT());
            }
            GUILayout.EndHorizontal();

            RelativityModeSelectUI();

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        double AngleToProgradeAtUT(double UT)
        {
            double angleToPrograde = 0;
            if (vessel.mainBody != Planetarium.fetch.Sun)
            {
                Orbit o = node.patch;
                Orbit refo = o.referenceBody.orbit;
                double rawAngleToPrograde = ((Vector3)o.Up(UT)).AngleInPlane(refo.NormalPlus(UT), refo.Prograde(UT));
                double retrogradeFactor = (((o.inclination > 90) || (o.inclination < -90)) ? 1 : -1);
                angleToPrograde = MuUtils.ClampDegrees360(retrogradeFactor * rawAngleToPrograde);
            }
            //print("node ATP = " + angleToPrograde);
            return angleToPrograde;
        }

        double progradeAngleChangeUT()
        {
            // Implicitly assuming a near circular orbit
            double UT = node.UT;
            double deltaTime = 0;
            double inputAngleToPrograde = inputAngleTo;
            if (retrogradeSelect == 1)
                inputAngleToPrograde += 180;

            if (vessel.mainBody != Planetarium.fetch.Sun)
            {
                Orbit o = node.patch;
                double currentUT = Planetarium.GetUniversalTime();
                if (UT < currentUT)
                {
                    UT = currentUT;
                }
                double deltaATP = MuUtils.ClampDegrees180(AngleToProgradeAtUT(UT) - inputAngleToPrograde);
                if (Math.Abs(deltaATP) > 0.01)
                {
                    Orbit refo = o.referenceBody.orbit;
                    Vector3d refNorm = refo.NormalPlus(UT);
                    double retrogradeFactor = (((o.inclination > 90) || (o.inclination < -90)) ? 1 : -1);
                    Vector3d newUp = Quaternion.AngleAxis((float)(-retrogradeFactor * deltaATP), refNorm) * o.Up(UT);
                    double newTrueAnomaly = o.TrueAnomalyFromVector(newUp);
                    deltaTime = o.TimeOfTrueAnomaly(newTrueAnomaly, UT - o.period/2) - UT;
                    if ((deltaTime < 0) && ((UT + deltaTime) <= currentUT))
                    {
                        deltaTime += o.period;
                    }
                }
            }
           return UT + deltaTime;
        }

        static readonly string[] relativityModeStrings = { "0", "1", "2", "3", "4" };
        private void RelativityModeSelectUI()
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Conics mode:", GUILayout.ExpandWidth(false));
            int newRelativityMode = GUILayout.SelectionGrid((int)vessel.patchedConicRenderer.relativityMode, relativityModeStrings, 5);
            vessel.patchedConicRenderer.relativityMode = (PatchRendering.RelativityMode)newRelativityMode;
            GUILayout.EndHorizontal();

            GUILayout.Label("Current mode: " + vessel.patchedConicRenderer.relativityMode.ToString());

            GUILayout.EndVertical();
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(300), GUILayout.Height(150) };
        }

        public override string GetName()
        {
            return "Maneuver Node Editor";
        }

        public MechJebModuleNodeEditor(MechJebCore core) : base(core) { }
    }
}
