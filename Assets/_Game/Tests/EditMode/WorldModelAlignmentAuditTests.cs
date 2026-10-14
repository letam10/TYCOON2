using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class WorldModelAlignmentAuditTests
    {
        [Test]
        public void CheckModelAcceptsGroundedPivotAndFiniteTransform()
        {
            GameObject root = CreateModel("GroundedModel", .5f);
            try
            {
                ModelAlignmentRecord record = WorldModelAlignmentAudit.CheckModel(root, .01f);

                Assert.That(record.Passed, Is.True, record.Error);
                Assert.That(record.HasRenderer, Is.True);
                Assert.That(record.Grounded, Is.True);
                Assert.That(record.PivotInsideFootprint, Is.True);
                Assert.That(record.ScaleFinite, Is.True);
                Assert.That(record.YawFinite, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CheckModelRejectsLiftedRendererAndReportsGroundContact()
        {
            GameObject root = CreateModel("LiftedModel", 1.5f);
            try
            {
                ModelAlignmentRecord record = WorldModelAlignmentAudit.CheckModel(root, .05f);

                Assert.That(record.Passed, Is.False);
                Assert.That(record.Grounded, Is.False);
                Assert.That(record.GroundOffset, Is.EqualTo(1f).Within(.0001f));
                StringAssert.Contains("ground", record.Error);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CheckModelRejectsMissingRendererAndZeroScale()
        {
            var missing = new GameObject("MissingRenderer");
            var zero = CreateModel("ZeroScaleModel", .5f);
            zero.transform.localScale = Vector3.zero;
            try
            {
                ModelAlignmentRecord missingRecord = WorldModelAlignmentAudit.CheckModel(missing, .05f);
                ModelAlignmentRecord zeroRecord = WorldModelAlignmentAudit.CheckModel(zero, .05f);

                Assert.That(missingRecord.Passed, Is.False);
                Assert.That(missingRecord.HasRenderer, Is.False);
                Assert.That(zeroRecord.Passed, Is.False);
                Assert.That(zeroRecord.ScaleFinite, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(missing);
                Object.DestroyImmediate(zero);
            }
        }

        [Test]
        public void CheckLabelAcceptsBillboardAnchorInsideFootprint()
        {
            var labelObject = new GameObject("BillboardLabel", typeof(TextMesh));
            labelObject.transform.position = new Vector3(1, 2, 1);
            var anchor = labelObject.AddComponent<WorldLabelAnchor>();
            anchor.Billboard = true;
            labelObject.AddComponent<BillboardLabel>();
            var footprint = new Bounds(Vector3.zero, new Vector3(4, 2, 4));
            try
            {
                LabelAlignmentRecord record = WorldModelAlignmentAudit.CheckLabel(labelObject.transform,
                    footprint, .25f);

                Assert.That(record.Passed, Is.True, record.Error);
                Assert.That(record.IsFinite, Is.True);
                Assert.That(record.InsideFootprint, Is.True);
                Assert.That(record.WithinDistance, Is.True);
                Assert.That(record.BillboardConfigured, Is.True);
                Assert.That(record.FacingFinite, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(labelObject);
            }
        }

        [Test]
        public void CheckLabelRejectsAnchorOutsideFootprintAndTooFar()
        {
            var labelObject = new GameObject("FarLabel");
            labelObject.transform.position = new Vector3(5, 1, 0);
            var anchor = labelObject.AddComponent<WorldLabelAnchor>();
            anchor.Billboard = false;
            var footprint = new Bounds(Vector3.zero, new Vector3(4, 2, 4));
            try
            {
                LabelAlignmentRecord record = WorldModelAlignmentAudit.CheckLabel(labelObject.transform,
                    footprint, .5f);

                Assert.That(record.Passed, Is.False);
                Assert.That(record.InsideFootprint, Is.False);
                Assert.That(record.WithinDistance, Is.False);
                Assert.That(record.FootprintDistance, Is.EqualTo(3f).Within(.0001f));
                StringAssert.Contains("outside", record.Error);
            }
            finally
            {
                Object.DestroyImmediate(labelObject);
            }
        }

        [Test]
        public void CheckLabelRejectsMissingFacingOrBillboardFlag()
        {
            var labelObject = new GameObject("UnregisteredLabel");
            labelObject.transform.position = new Vector3(0, 1, 0);
            var footprint = new Bounds(Vector3.zero, new Vector3(4, 2, 4));
            try
            {
                LabelAlignmentRecord record = WorldModelAlignmentAudit.CheckLabel(labelObject.transform,
                    footprint, .25f);

                Assert.That(record.Passed, Is.False);
                Assert.That(record.BillboardConfigured, Is.False);
                StringAssert.Contains("facing or billboard", record.Error);
            }
            finally
            {
                Object.DestroyImmediate(labelObject);
            }
        }

        [Test]
        public void ValidateAggregatesModelAndLabelRecords()
        {
            GameObject root = CreateModel("ValidationModel", .5f);
            var labelObject = new GameObject("ValidationLabel");
            labelObject.transform.position = new Vector3(0, 1, 0);
            var marker = labelObject.AddComponent<WorldLabelAnchor>();
            marker.Billboard = false;
            var footprint = new Bounds(Vector3.zero, new Vector3(4, 2, 4));
            try
            {
                ModelAlignmentRecord modelRecord = WorldModelAlignmentAudit.CheckModel(root, .01f);
                LabelAlignmentRecord labelRecord = WorldModelAlignmentAudit.CheckLabel(labelObject.transform,
                    footprint, .25f);
                var records = new List<Record> { modelRecord, labelRecord };

                Assert.That(WorldModelAlignmentAudit.Validate(records, out string passSummary), Is.True);
                StringAssert.Contains("passed", passSummary);

                labelObject.transform.position = new Vector3(6, 1, 0);
                labelRecord = WorldModelAlignmentAudit.CheckLabel(labelObject.transform, footprint, .25f);
                records[1] = labelRecord;
                Assert.That(WorldModelAlignmentAudit.Validate(records, out string failSummary), Is.False);
                StringAssert.Contains("ValidationLabel", failSummary);
                StringAssert.Contains("failed", failSummary);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(labelObject);
            }
        }

        [Test]
        public void ArtLabelRegistersModelAnchorAndBillboardMetadata()
        {
            GameCatalog previousCatalog = Art.Catalog;
            Art.Catalog = ScriptableObject.CreateInstance<GameCatalog>();
            var parent = new GameObject("LabelModelRoot");
            try
            {
                TextMesh label = Art.Label("TEST", new Vector3(0, 1, 0), parent.transform,
                    .1f, "#FFFFFF", true);
                WorldLabelAnchor anchor = label.GetComponent<WorldLabelAnchor>();

                Assert.That(anchor, Is.Not.Null);
                Assert.That(anchor.Billboard, Is.True);
                Assert.That(anchor.ModelRoot, Is.EqualTo(parent.transform));
                Assert.That(label.GetComponent<BillboardLabel>(), Is.Not.Null);
            }
            finally
            {
                Art.Catalog = previousCatalog;
                Object.DestroyImmediate(parent);
            }
        }

        static GameObject CreateModel(string name, float bodyHeight)
        {
            var root = new GameObject(name);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0, bodyHeight, 0);
            return root;
        }
    }
}
