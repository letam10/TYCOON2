using NUnit.Framework;
using UnityEngine;

namespace Tycoon.Tests
{
    public sealed class MapViewportStateContractTests
    {
        [Test]
        public void DefaultStateIsDeterministicAndValid()
        {
            var state = new MapViewportState();

            Assert.That(state.Center, Is.EqualTo(Vector3.zero));
            Assert.That(state.Zoom, Is.EqualTo(1f));
            Assert.That(state.RotationDegrees, Is.EqualTo(0f));
            Assert.That(state.SelectedPoiId, Is.Null);
            Assert.That(state.EnabledKinds, Is.Empty);
            Assert.That(state.IsKindEnabled("farm"), Is.True);
            Assert.That(state.IsFiniteAndValid(), Is.True);
        }

        [Test]
        public void ZoomIsClampedAndNonFiniteAmountsAreIgnored()
        {
            var state = new MapViewportState();

            state.ZoomBy(100f);
            Assert.That(state.Zoom, Is.EqualTo(MapViewportState.MaxZoom));

            state.ZoomBy(-100f);
            Assert.That(state.Zoom, Is.EqualTo(MapViewportState.MinZoom));

            state.ZoomBy(float.NaN);
            state.ZoomBy(float.PositiveInfinity);
            state.ZoomBy(float.NegativeInfinity);
            Assert.That(state.Zoom, Is.EqualTo(MapViewportState.MinZoom));
            Assert.That(state.IsFiniteAndValid(), Is.True);
        }

        [Test]
        public void PanUsesNormalizedXAndZAndFocusSetsPlayerPosition()
        {
            var state = new MapViewportState();
            state.SetCenter(new Vector3(10f, 4f, -3f));

            state.Pan(new Vector2(2f, -5f));
            Assert.That(state.Center, Is.EqualTo(new Vector3(12f, 4f, -8f)));

            state.FocusPlayer(new Vector3(-20f, .5f, 30f));
            Assert.That(state.Center, Is.EqualTo(new Vector3(-20f, .5f, 30f)));
            Assert.That(state.IsFiniteAndValid(), Is.True);
        }

        [Test]
        public void NonFiniteCenterAndPanInputsDoNotChangeState()
        {
            var state = new MapViewportState();
            state.SetCenter(new Vector3(10f, 4f, -3f));
            Vector3 expected = state.Center;

            state.SetCenter(new Vector3(float.NaN, 0f, 0f));
            state.FocusPlayer(new Vector3(0f, float.PositiveInfinity, 0f));
            state.Pan(new Vector2(float.NaN, 1f));
            state.Pan(new Vector2(1f, float.NegativeInfinity));

            Assert.That(state.Center, Is.EqualTo(expected));
            Assert.That(state.IsFiniteAndValid(), Is.True);
        }

        [Test]
        public void PanRejectsFiniteInputThatWouldOverflowCenter()
        {
            var state = new MapViewportState();
            state.SetCenter(new Vector3(float.MaxValue, 0f, 0f));
            Vector3 expected = state.Center;

            state.Pan(new Vector2(float.MaxValue, 0f));

            Assert.That(state.Center, Is.EqualTo(expected));
            Assert.That(state.IsFiniteAndValid(), Is.True);
        }

        [Test]
        public void RotationIsNormalizedToHalfOpenNorthRange()
        {
            var state = new MapViewportState();

            state.RotateBy(725f);
            Assert.That(state.RotationDegrees, Is.EqualTo(5f));

            state.RotateBy(-190f);
            Assert.That(state.RotationDegrees, Is.EqualTo(175f));

            state.RotateBy(float.NaN);
            state.RotateBy(float.PositiveInfinity);
            Assert.That(state.RotationDegrees, Is.EqualTo(175f));

            state.ResetNorth();
            Assert.That(state.RotationDegrees, Is.EqualTo(0f));
            Assert.That(state.IsFiniteAndValid(), Is.True);
        }

        [Test]
        public void SelectionAndKindFiltersUseOrdinalStableValues()
        {
            var state = new MapViewportState();

            state.Select("station");
            Assert.That(state.SelectedPoiId, Is.EqualTo("station"));
            state.Select(string.Empty);
            Assert.That(state.SelectedPoiId, Is.Null);

            state.ToggleKind("Forest");
            Assert.That(state.IsKindEnabled("Forest"), Is.True);
            Assert.That(state.IsKindEnabled("forest"), Is.False);
            Assert.That(state.IsKindEnabled("farm"), Is.False);

            state.ToggleKind("forest");
            Assert.That(state.EnabledKinds, Has.Count.EqualTo(2));
            state.ToggleKind("Forest");
            Assert.That(state.IsKindEnabled("Forest"), Is.False);
            Assert.That(state.IsKindEnabled("forest"), Is.True);

            state.ToggleKind("forest");
            Assert.That(state.EnabledKinds, Is.Empty);
            Assert.That(state.IsKindEnabled("farm"), Is.True);
            Assert.That(state.IsFiniteAndValid(), Is.True);
        }

        [Test]
        public void InvalidSelectionAndFilterInputsAreIgnored()
        {
            var state = new MapViewportState();
            state.Select("station");

            state.Select(null);
            state.ToggleKind(null);
            state.ToggleKind(string.Empty);

            Assert.That(state.SelectedPoiId, Is.Null);
            Assert.That(state.EnabledKinds, Is.Empty);
            Assert.That(state.IsKindEnabled("station"), Is.True);
            Assert.That(state.IsFiniteAndValid(), Is.True);
        }

        [Test]
        public void ViewportOperationsPreserveStablePoiIds()
        {
            string[] expectedIds = new string[CityMapLocations.Points.Length];
            for (int index = 0; index < CityMapLocations.Points.Length; index++)
                expectedIds[index] = CityMapLocations.Points[index].id;

            var state = new MapViewportState();
            state.SetCenter(new Vector3(15f, .5f, -22f));
            state.FocusPlayer(new Vector3(-7f, .5f, 31f));
            state.Pan(new Vector2(.25f, -.5f));
            state.ZoomBy(.75f);
            state.RotateBy(45f);
            state.Select("forest_reserve");
            state.ToggleKind("forest");

            Assert.That(CityMapLocations.Points, Has.Length.EqualTo(expectedIds.Length));
            for (int index = 0; index < expectedIds.Length; index++)
                Assert.That(CityMapLocations.Points[index].id, Is.EqualTo(expectedIds[index]));
            Assert.That(state.IsFiniteAndValid(), Is.True);
        }
    }
}
