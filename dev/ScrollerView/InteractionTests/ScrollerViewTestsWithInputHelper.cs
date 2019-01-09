﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

using Common;
using Windows.UI.Xaml.Tests.MUXControls.InteractionTests.Infra;
using Windows.UI.Xaml.Tests.MUXControls.InteractionTests.Common;
using System.Threading;

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
#endif

#if BUILD_WINDOWS
using System.Windows.Automation;
using MS.Internal.Mita.Foundation;
using MS.Internal.Mita.Foundation.Controls;
using MS.Internal.Mita.Foundation.Patterns;
using MS.Internal.Mita.Foundation.Waiters;
using Point = MS.Internal.Mita.Foundation.PointI;
#else
using Microsoft.Windows.Apps.Test.Automation;
using Microsoft.Windows.Apps.Test.Foundation;
using Microsoft.Windows.Apps.Test.Foundation.Controls;
using Microsoft.Windows.Apps.Test.Foundation.Patterns;
using Microsoft.Windows.Apps.Test.Foundation.Waiters;
using Point = System.Drawing.Point;
#endif

namespace Windows.UI.Xaml.Tests.MUXControls.InteractionTests
{
    [TestClass]
    public class ScrollerViewTestsWithInputHelper
    {
        [ClassInitialize]
        [TestProperty("RunAs", "User")]
        [TestProperty("Classification", "Integration")]
        [TestProperty("Platform", "Any")]
        public static void ClassInitialize(TestContext testContext)
        {
            TestEnvironment.Initialize(testContext);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TestCleanupHelper.Cleanup();
        }

        [TestMethod]
        [TestProperty("Description", "Pans an Image in a ScrollerView.")]
        public void PanScrollerView()
        {
            if (PlatformConfiguration.IsOSVersionLessThan(OSVersion.Redstone5))
            {
                Log.Warning("This test relies on touch input, the injection of which is only supported in RS5 and up. Test is disabled.");
                return;
            }

            const double minHorizontalScrollPercent = 35.0;
            const double minVerticalScrollPercent = 35.0;

            Log.Comment("Selecting ScrollerView tests");

            using (IDisposable setup = new TestSetupHelper("ScrollerView Tests"),
                               setup2 = new TestSetupHelper("navigateToSimpleContents"))
            {
                Log.Comment("Retrieving cmbShowScrollerView");
                ComboBox cmbShowScrollerView = new ComboBox(FindElement.ByName("cmbShowScrollerView"));
                Verify.IsNotNull(cmbShowScrollerView, "Verifying that cmbShowScrollerView was found");

                Log.Comment("Changing ScrollerView selection to scrollerView51");
                cmbShowScrollerView.SelectItemByName("scrollerView_51");
                Log.Comment("Selection is now {0}", cmbShowScrollerView.Selection[0].Name);

                if (PlatformConfiguration.IsOsVersion(OSVersion.Redstone1))
                {
                    Log.Comment("On RS1 the Scroller's child is centered in an animated way when it's smaller than the viewport. Waiting for those animations to complete.");
                    WaitForScrollerViewManipulationEnd("scrollerView21");
                }

                Log.Comment("Retrieving img51");
                UIObject img51UIObject = FindElement.ByName("img51");
                Verify.IsNotNull(img51UIObject, "Verifying that img51 was found");

                Log.Comment("Retrieving scroller51");
                Scroller scroller51 = new Scroller(img51UIObject.Parent);
                Verify.IsNotNull(scroller51, "Verifying that scroller51 was found");

                WaitForScrollerViewFinalSize(scroller51, 300.0 /*expectedWidth*/, 400.0 /*expectedHeight*/);

                // Tapping button before attempting pan operation to guarantee effective touch input
                TapResetViewsButton();

                Log.Comment("Panning ScrollerView in diagonal");
                PrepareForScrollerViewManipulationStart();

                InputHelper.Pan(
                    scroller51,
                    new Point(scroller51.BoundingRectangle.Left + 25, scroller51.BoundingRectangle.Top + 25),
                    new Point(scroller51.BoundingRectangle.Left - 25, scroller51.BoundingRectangle.Top - 25));

                Log.Comment("Waiting for scrollerView51 pan completion");
                WaitForScrollerViewManipulationEnd("scrollerView51");

                Log.Comment("scroller51.HorizontalScrollPercent={0}", scroller51.HorizontalScrollPercent);
                Log.Comment("scroller51.VerticalScrollPercent={0}", scroller51.VerticalScrollPercent);

                if (scroller51.HorizontalScrollPercent <= minHorizontalScrollPercent || scroller51.VerticalScrollPercent <= minVerticalScrollPercent)
                {
                    LogAndClearTraces();
                }

                Verify.IsTrue(scroller51.HorizontalScrollPercent > minHorizontalScrollPercent, "Verifying scroller51 HorizontalScrollPercent is greater than " + minHorizontalScrollPercent + "%");
                Verify.IsTrue(scroller51.VerticalScrollPercent > minVerticalScrollPercent, "Verifying scroller51 VerticalScrollPercent is greater than " + minVerticalScrollPercent + "%");

                // scroller51's Child size is 800x800px.
                double horizontalOffset;
                double verticalOffset;
                double minHorizontalOffset = 800.0 * (1.0 - scroller51.HorizontalViewSize / 100.0) * minHorizontalScrollPercent / 100.0;
                double minVerticalOffset = 800.0 * (1.0 - scroller51.VerticalViewSize / 100.0) * minVerticalScrollPercent / 100.0;
                float zoomFactor;

                GetScrollerView(out horizontalOffset, out verticalOffset, out zoomFactor);
                Log.Comment("horizontalOffset={0}", horizontalOffset);
                Log.Comment("verticalOffset={0}", verticalOffset);
                Log.Comment("zoomFactor={0}", zoomFactor);
                Verify.IsTrue(horizontalOffset > minHorizontalOffset, "Verifying horizontalOffset is greater than " + minHorizontalOffset);
                Verify.IsTrue(verticalOffset > minVerticalOffset, "Verifying verticalOffset is greater than " + minVerticalOffset);
                Verify.AreEqual(zoomFactor, 1.0f, "Verifying zoomFactor is 1.0f");

                // Output-debug-string-level "None" is automatically restored when landing back on the ScrollerView test page.
            }
        }

        [TestMethod]
        [TestProperty("Description", "Scrolls an Image in a ScrollerView using the mouse on the ScrollBar2 thumb, then pans it with touch.")]
        public void ScrollThenPanScrollerView()
        {
            if (PlatformConfiguration.IsOSVersionLessThan(OSVersion.Redstone5))
            {
                Log.Warning("This test relies on touch input, the injection of which is only supported in RS5 and up. Test is disabled.");
                return;
            }

            Log.Comment("Selecting ScrollerView tests");

            using (IDisposable setup = new TestSetupHelper("ScrollerView Tests"),
                               setup2 = new TestSetupHelper("navigateToSimpleContents"))
            {
                const double minVerticalScrollPercentAfterScroll = 15.0;
                const double minHorizontalScrollPercentAfterPan = 35.0;
                const double minVerticalScrollPercentAfterPan = 50.0;

                double verticalScrollPercentAfterScroll = 0.0;

                Log.Comment("Retrieving cmbShowScrollerView");
                ComboBox cmbShowScrollerView = new ComboBox(FindElement.ByName("cmbShowScrollerView"));
                Verify.IsNotNull(cmbShowScrollerView, "Verifying that cmbShowScrollerView was found");

                Log.Comment("Changing ScrollerView selection to scrollerView51");
                cmbShowScrollerView.SelectItemByName("scrollerView_51");
                Log.Comment("Selection is now {0}", cmbShowScrollerView.Selection[0].Name);

                if (PlatformConfiguration.IsOsVersion(OSVersion.Redstone1))
                {
                    Log.Comment("On RS1 the Scroller's child is centered in an animated way when it's smaller than the viewport. Waiting for those animations to complete.");
                    WaitForScrollerViewManipulationEnd("scrollerView21");
                }

                Log.Comment("Retrieving img51");
                UIObject img51UIObject = FindElement.ByName("img51");
                Verify.IsNotNull(img51UIObject, "Verifying that img51 was found");

                Log.Comment("Retrieving scroller51");
                Scroller scroller51 = new Scroller(img51UIObject.Parent);
                Verify.IsNotNull(scroller51, "Verifying that scroller51 was found");                

                WaitForScrollerViewFinalSize(scroller51, 300.0 /*expectedWidth*/, 400.0 /*expectedHeight*/);

                // Tapping button before attempting pan operation to guarantee effective touch input
                TapResetViewsButton();

                Log.Comment("Left mouse buttom down over ScrollBar2 thumb");
                InputHelper.LeftMouseButtonDown(scroller51, 140 /*offsetX*/, -100 /*offsetY*/);

                Log.Comment("Mouse drag and left mouse buttom up over ScrollBar2 thumb");
                InputHelper.LeftMouseButtonUp(scroller51, 140 /*offsetX*/, -50 /*offsetY*/);

                Log.Comment("scroller51.HorizontalScrollPercent={0}", scroller51.HorizontalScrollPercent);
                Log.Comment("scroller51.VerticalScrollPercent={0}", scroller51.VerticalScrollPercent);

                verticalScrollPercentAfterScroll = scroller51.VerticalScrollPercent;

                if (scroller51.HorizontalScrollPercent != 0.0 || scroller51.VerticalScrollPercent <= minVerticalScrollPercentAfterScroll)
                {
                    LogAndClearTraces();
                }

                Verify.AreEqual(scroller51.HorizontalScrollPercent, 0.0, "Verifying scroller51 HorizontalScrollPercent is still 0%");
                Verify.IsTrue(verticalScrollPercentAfterScroll > minVerticalScrollPercentAfterScroll, "Verifying scroller51 VerticalScrollPercent is greater than " + minVerticalScrollPercentAfterScroll + "%");

                Log.Comment("Panning ScrollerView in diagonal");
                PrepareForScrollerViewManipulationStart();

                // Using a large enough span and duration for this diagonal pan so that it is not erroneously recognized as a horizontal pan.
                InputHelper.Pan(
                    scroller51,
                    new Point(scroller51.BoundingRectangle.Left + 30, scroller51.BoundingRectangle.Top + 30),
                    new Point(scroller51.BoundingRectangle.Left - 30, scroller51.BoundingRectangle.Top - 30),
                    InputHelper.DefaultPanHoldDuration,
                    InputHelper.DefaultPanAcceleration / 2.4f);

                Log.Comment("Waiting for scrollerView51 pan completion");
                WaitForScrollerViewManipulationEnd("scrollerView51");

                Log.Comment("scroller51.HorizontalScrollPercent={0}", scroller51.HorizontalScrollPercent);
                Log.Comment("scroller51.VerticalScrollPercent={0}", scroller51.VerticalScrollPercent);

                if (scroller51.HorizontalScrollPercent <= minHorizontalScrollPercentAfterPan ||
                    scroller51.VerticalScrollPercent <= minVerticalScrollPercentAfterPan ||
                    scroller51.VerticalScrollPercent <= verticalScrollPercentAfterScroll)
                {
                    LogAndClearTraces();
                }

                Verify.IsTrue(scroller51.HorizontalScrollPercent > minHorizontalScrollPercentAfterPan, "Verifying scroller51 HorizontalScrollPercent is greater than " + minHorizontalScrollPercentAfterPan + "%");
                Verify.IsTrue(scroller51.VerticalScrollPercent > minVerticalScrollPercentAfterPan, "Verifying scroller51 VerticalScrollPercent is greater than " + minVerticalScrollPercentAfterPan + "%");
                Verify.IsTrue(scroller51.VerticalScrollPercent > verticalScrollPercentAfterScroll, "Verifying scroller51 VerticalScrollPercent is greater than " + verticalScrollPercentAfterScroll + "%");

                // scroller51's Child size is 800x800px.
                double horizontalOffset;
                double verticalOffset;
                double minHorizontalOffset = 800.0 * (1.0 - scroller51.HorizontalViewSize / 100.0) * minHorizontalScrollPercentAfterPan / 100.0;
                double minVerticalOffset = 800.0 * (1.0 - scroller51.VerticalViewSize / 100.0) * minVerticalScrollPercentAfterPan / 100.0;
                float zoomFactor;

                GetScrollerView(out horizontalOffset, out verticalOffset, out zoomFactor);
                Log.Comment("horizontalOffset={0}", horizontalOffset);
                Log.Comment("verticalOffset={0}", verticalOffset);
                Log.Comment("zoomFactor={0}", zoomFactor);
                Verify.IsTrue(horizontalOffset > minHorizontalOffset, "Verifying horizontalOffset is greater than " + minHorizontalOffset);
                Verify.IsTrue(verticalOffset > minVerticalOffset, "Verifying verticalOffset is greater than " + minVerticalOffset);
                Verify.AreEqual(zoomFactor, 1.0f, "Verifying zoomFactor is 1.0f");
            }
        }

        [TestMethod]
        [TestProperty("Description", "Tests Keyboard interaction (Up, Down, Left, Right, PageUp, PageDown, Home, End)")]
        public void VerifyScrollerViewKeyboardInteraction()
        {
            if (PlatformConfiguration.IsOSVersionLessThan(OSVersion.Redstone2))
            {
                Log.Warning("Test is disabled on pre-RS2 because ScrollerView not supported pre-RS2");
                return;
            }

            using (IDisposable setup = new TestSetupHelper("ScrollerView Tests"),
                               setup2 = new TestSetupHelper("navigateToSimpleContents"))
            {
                UIObject buttonInScrollerView11;
                Scroller scroller11;
                SetupSimpleSingleScrollerViewTest(out buttonInScrollerView11, out scroller11);

                var scrollAmountForDownOrUpKey = scroller11.BoundingRectangle.Height * 0.15;
                var scrollAmountForPageUpOrPageDownKey = scroller11.BoundingRectangle.Height;
                var scrollAmountForRightOrLeftKey = scroller11.BoundingRectangle.Width * 0.15;
                var maxScrollOffset = 2000 - scroller11.BoundingRectangle.Height;

                double expectedVerticalOffset = 0;
                double expectedHorizontalOffset = 0;

                Log.Comment("Pressing Down key");
                KeyboardHelper.PressKey(buttonInScrollerView11, Key.Down, modifierKey: ModifierKey.None, numPresses: 1, useDebugMode: true);
                expectedVerticalOffset += scrollAmountForDownOrUpKey;
                WaitForScrollerViewOffsets(expectedHorizontalOffset, expectedVerticalOffset);

                Log.Comment("Pressing PageDown key");
                KeyboardHelper.PressKey(buttonInScrollerView11, Key.PageDown, modifierKey: ModifierKey.None, numPresses: 1, useDebugMode: true);
                expectedVerticalOffset += scrollAmountForPageUpOrPageDownKey;
                WaitForScrollerViewOffsets(expectedHorizontalOffset, expectedVerticalOffset);

                Log.Comment("Pressing Home key");
                KeyboardHelper.PressKey(buttonInScrollerView11, Key.Home, modifierKey: ModifierKey.None, numPresses: 1, useDebugMode: true);
                expectedVerticalOffset = 0;
                WaitForScrollerViewOffsets(expectedHorizontalOffset, expectedVerticalOffset);

                Log.Comment("Pressing End key");
                KeyboardHelper.PressKey(buttonInScrollerView11, Key.End, modifierKey: ModifierKey.None, numPresses: 1, useDebugMode: true);
                expectedVerticalOffset = maxScrollOffset;
                WaitForScrollerViewOffsets(expectedHorizontalOffset, expectedVerticalOffset);

                Log.Comment("Pressing Up key");
                KeyboardHelper.PressKey(buttonInScrollerView11, Key.Up, modifierKey: ModifierKey.None, numPresses: 1, useDebugMode: true);
                expectedVerticalOffset -= scrollAmountForDownOrUpKey;
                WaitForScrollerViewOffsets(expectedHorizontalOffset, expectedVerticalOffset);

                Log.Comment("Pressing PageUp key");
                KeyboardHelper.PressKey(buttonInScrollerView11, Key.PageUp, modifierKey: ModifierKey.None, numPresses: 1, useDebugMode: true);
                expectedVerticalOffset -= scrollAmountForPageUpOrPageDownKey;
                WaitForScrollerViewOffsets(expectedHorizontalOffset, expectedVerticalOffset);

                Log.Comment("Pressing Right key");
                KeyboardHelper.PressKey(buttonInScrollerView11, Key.Right, modifierKey: ModifierKey.None, numPresses: 1, useDebugMode: true);
                expectedHorizontalOffset += scrollAmountForRightOrLeftKey;
                WaitForScrollerViewOffsets(expectedHorizontalOffset, expectedVerticalOffset);

                Log.Comment("Pressing Left key");
                KeyboardHelper.PressKey(buttonInScrollerView11, Key.Left, modifierKey: ModifierKey.None, numPresses: 1, useDebugMode: true);
                expectedHorizontalOffset -= scrollAmountForRightOrLeftKey;
                WaitForScrollerViewOffsets(expectedHorizontalOffset, expectedVerticalOffset);

                Log.Comment("Pressing Down key three times");
                KeyboardHelper.PressKey(buttonInScrollerView11, Key.Down, modifierKey: ModifierKey.None, numPresses: 3, useDebugMode: true);
                expectedVerticalOffset += 3 * scrollAmountForDownOrUpKey;
                WaitForScrollerViewOffsets(expectedHorizontalOffset, expectedVerticalOffset);
            }
        }

        [TestMethod]
        [TestProperty("Description", "Tests keyboard interaction (Down, Up, PageDown, PageUp, End, Home, Right, Left) when ScrollerView.XYFocusKeyboardNavigation is Enabled.")]
        public void VerifyScrollerViewKeyboardInteractionWithXYFocusEnabled()
        {
            if (PlatformConfiguration.IsOSVersionLessThan(OSVersion.Redstone2))
            {
                Log.Warning("Test is disabled on pre-RS2 because ScrollerView not supported pre-RS2");
                return;
            }

            using (IDisposable setup = new TestSetupHelper("ScrollerView Tests"),
                               setup2 = new TestSetupHelper("navigateToSimpleContents"))
            {
                UIObject img51;
                Scroller scroller51;

                SetupScrollerViewTestWithImage("51", out img51, out scroller51);

                var scrollAmountForDownOrUpKey = scroller51.BoundingRectangle.Height * 0.5;
                var scrollAmountForPageUpOrPageDownKey = scroller51.BoundingRectangle.Height;
                var scrollAmountForRightOrLeftKey = scroller51.BoundingRectangle.Width * 0.5;
                var maxScrollOffset = 800 - scroller51.BoundingRectangle.Height;

                Log.Comment("Pressing Down key");
                KeyboardHelper.PressKey(scroller51, Key.Down, modifierKey: ModifierKey.None, numPresses: 1, useDebugMode: true);
                WaitForScrollerViewOffsets(0, scrollAmountForDownOrUpKey);

                Log.Comment("Pressing Up key");
                KeyboardHelper.PressKey(scroller51, Key.Up, modifierKey: ModifierKey.None, numPresses: 1, useDebugMode: true);
                WaitForScrollerViewOffsets(0, 0);

                Log.Comment("Pressing PageDown key");
                KeyboardHelper.PressKey(scroller51, Key.PageDown, modifierKey: ModifierKey.None, numPresses: 1, useDebugMode: true);
                WaitForScrollerViewOffsets(0, scrollAmountForPageUpOrPageDownKey);

                Log.Comment("Pressing PageUp key");
                KeyboardHelper.PressKey(scroller51, Key.PageUp, modifierKey: ModifierKey.None, numPresses: 1, useDebugMode: true);
                WaitForScrollerViewOffsets(0, 0);

                Log.Comment("Pressing End key");
                KeyboardHelper.PressKey(scroller51, Key.End, modifierKey: ModifierKey.None, numPresses: 1, useDebugMode: true);
                WaitForScrollerViewOffsets(0, maxScrollOffset);

                Log.Comment("Pressing Home key");
                KeyboardHelper.PressKey(scroller51, Key.Home, modifierKey: ModifierKey.None, numPresses: 1, useDebugMode: true);
                WaitForScrollerViewOffsets(0, 0);

                Log.Comment("Pressing Right key");
                KeyboardHelper.PressKey(scroller51, Key.Right, modifierKey: ModifierKey.None, numPresses: 1, useDebugMode: true);
                WaitForScrollerViewOffsets(scrollAmountForRightOrLeftKey, 0);

                Log.Comment("Pressing Left key");
                KeyboardHelper.PressKey(scroller51, Key.Left, modifierKey: ModifierKey.None, numPresses: 1, useDebugMode: true);
                WaitForScrollerViewOffsets(0, 0);
            }
        }

        [TestMethod]
        [TestProperty("Description", "Tests End and Home keys when ScrollerView.VerticalScrollMode is Disabled.")]
        public void ScrollHorizontallyWithEndHomeKeys()
        {
            if (PlatformConfiguration.IsOSVersionLessThan(OSVersion.Redstone2))
            {
                Log.Warning("Test is disabled on pre-RS2 because ScrollerView not supported pre-RS2");
                return;
            }

            using (IDisposable setup = new TestSetupHelper("ScrollerView Tests"),
                               setup2 = new TestSetupHelper("navigateToSimpleContents"))
            {
                UIObject img31;
                Scroller scroller31;

                SetupScrollerViewTestWithImage("31", out img31, out scroller31);

                Log.Comment("Pressing End key");
                KeyboardHelper.PressKey(scroller31, Key.End, modifierKey: ModifierKey.None, numPresses: 1, useDebugMode: true);
                WaitForScrollerViewOffsets(900 - scroller31.BoundingRectangle.Width, 0);

                Log.Comment("Pressing Home key");
                KeyboardHelper.PressKey(scroller31, Key.Home, modifierKey: ModifierKey.None, numPresses: 1, useDebugMode: true);
                WaitForScrollerViewOffsets(0, 0);
            }
        }

        [TestMethod]
        [TestProperty("Description", "Tests End and Home keys when ScrollerView.VerticalScrollMode is Disabled in RightToLeft flow direction.")]
        public void ScrollHorizontallyWithEndHomeKeysInRTL()
        {
            if (PlatformConfiguration.IsOSVersionLessThan(OSVersion.Redstone2))
            {
                Log.Warning("Test is disabled on pre-RS2 because ScrollerView not supported pre-RS2");
                return;
            }

            using (IDisposable setup = new TestSetupHelper("ScrollerView Tests"),
                               setup2 = new TestSetupHelper("navigateToSimpleContents"))
            {
                UIObject img32;
                Scroller scroller32;

                SetupScrollerViewTestWithImage("32", out img32, out scroller32);

                Log.Comment("Pressing Home key");
                KeyboardHelper.PressKey(scroller32, Key.Home, modifierKey: ModifierKey.None, numPresses: 1, useDebugMode: true);
                WaitForScrollerViewOffsets(900 - scroller32.BoundingRectangle.Width, 0);

                Log.Comment("Pressing End key");
                KeyboardHelper.PressKey(scroller32, Key.End, modifierKey: ModifierKey.None, numPresses: 1, useDebugMode: true);
                WaitForScrollerViewOffsets(0, 0);
            }
        }

        //Unreliable test: ScrollerViewTestsWithInputHelper.VerifyScrollerViewGamePadInteraction #156
        //[TestMethod]
        //[TestProperty("Description", "Tests GamePad interaction")]
        public void VerifyScrollerViewGamePadInteraction()
        {
            if (PlatformConfiguration.IsOSVersionLessThan(OSVersion.Redstone4))
            {
                Log.Warning("Test is disabled on pre-RS4 because ScrollerView Gamepad interaction is not supported pre-RS4");
                return;
            }
            
            using (IDisposable setup = new TestSetupHelper("ScrollerView Tests"),
                               setup2 = new TestSetupHelper("navigateToSimpleContents"))
            {
                UIObject buttonInScrollerView11;
                Scroller scroller11;
                SetupSimpleSingleScrollerViewTest(out buttonInScrollerView11, out scroller11);
                
                var scrollAmountForGamepadUpDown = scroller11.BoundingRectangle.Height * 0.5;

                double expectedVerticalOffset = 0;
                double expectedHorizontalOffset = 0;

                //Down. Change focus. Don't scroll.
                PressGamepadButtonAndVerifyOffsetAndFocus(GamepadButton.LeftThumbstickDown, "Button 2", expectedHorizontalOffset, expectedVerticalOffset);

                //Down. Change focus. Scroll.
                expectedVerticalOffset = 220;
                PressGamepadButtonAndVerifyOffsetAndFocus(GamepadButton.LeftThumbstickDown, "Button 3", expectedHorizontalOffset, expectedVerticalOffset);

                //Down. Don't change focus. Scroll.
                expectedVerticalOffset += scrollAmountForGamepadUpDown;
                PressGamepadButtonAndVerifyOffsetAndFocus(GamepadButton.LeftThumbstickDown, "Button 3", expectedHorizontalOffset, expectedVerticalOffset);

                //Down. Change focus. Scroll.
                expectedVerticalOffset = 920;
                PressGamepadButtonAndVerifyOffsetAndFocus(GamepadButton.LeftThumbstickDown, "Button 4", expectedHorizontalOffset, expectedVerticalOffset);

                //Down. Change focus. Scroll.
                expectedVerticalOffset = 1020;
                PressGamepadButtonAndVerifyOffsetAndFocus(GamepadButton.LeftThumbstickDown, "Button 5", expectedHorizontalOffset, expectedVerticalOffset);

                //Up. Change focus. Don't scroll.
                PressGamepadButtonAndVerifyOffsetAndFocus(GamepadButton.LeftThumbstickUp, "Button 4", expectedHorizontalOffset, expectedVerticalOffset);

                //Up. Don't change focus. Scroll.
                expectedVerticalOffset -= scrollAmountForGamepadUpDown;
                PressGamepadButtonAndVerifyOffsetAndFocus(GamepadButton.LeftThumbstickUp, "Button 4", expectedHorizontalOffset, expectedVerticalOffset);

                //Up. Change focus. Scroll.
                expectedVerticalOffset = 480;
                PressGamepadButtonAndVerifyOffsetAndFocus(GamepadButton.LeftThumbstickUp, "Button 3", expectedHorizontalOffset, expectedVerticalOffset);
            }
        }

        [TestMethod]
        [TestProperty("Description", "Tests GamePad interaction")]
        public void VerifyScrollerViewGamePadHorizontalInteraction()
        {
            if (PlatformConfiguration.IsOSVersionLessThan(OSVersion.Redstone4))
            {
                Log.Warning("Test is disabled on pre-RS4 because ScrollerView Gamepad interaction is not supported pre-RS4");
                return;
            }
            
            using (IDisposable setup = new TestSetupHelper("ScrollerView Tests"),
                               setup2 = new TestSetupHelper("navigateToSimpleContents"))
            {
                UIObject buttonInScrollerView11;
                Scroller scroller11;
                SetupSimpleSingleScrollerViewTest(out buttonInScrollerView11, out scroller11);

                var scrollAmountForGamepadLeftRight = scroller11.BoundingRectangle.Width * 0.5;

                double expectedVerticalOffset = 0;
                double expectedHorizontalOffset = 0;

                //Right. Change focus. Don't scroll. 
                PressGamepadButtonAndVerifyOffsetAndFocus(GamepadButton.LeftThumbstickRight, "Button 1B", expectedHorizontalOffset, expectedVerticalOffset);

                //Left. Change focus. Don't scroll. 
                PressGamepadButtonAndVerifyOffsetAndFocus(GamepadButton.LeftThumbstickLeft, "Button 1", expectedHorizontalOffset, expectedVerticalOffset);

                //Right. Change focus. Don't scroll. 
                PressGamepadButtonAndVerifyOffsetAndFocus(GamepadButton.LeftThumbstickRight, "Button 1B", expectedHorizontalOffset, expectedVerticalOffset);

                //Right. Change focus. Scroll. 
                expectedHorizontalOffset = 320;
                PressGamepadButtonAndVerifyOffsetAndFocus(GamepadButton.LeftThumbstickRight, "Button 1C", expectedHorizontalOffset, expectedVerticalOffset);

                //Right. Don't Change focus. Scroll. 
                expectedHorizontalOffset += scrollAmountForGamepadLeftRight;
                PressGamepadButtonAndVerifyOffsetAndFocus(GamepadButton.LeftThumbstickRight, "Button 1C", expectedHorizontalOffset, expectedVerticalOffset);

                //Right. Don't Change focus. Scroll. 
                expectedHorizontalOffset += scrollAmountForGamepadLeftRight;
                PressGamepadButtonAndVerifyOffsetAndFocus(GamepadButton.LeftThumbstickRight, "Button 1C", expectedHorizontalOffset, expectedVerticalOffset);

                //Left. Don't Change focus. Scroll. 
                expectedHorizontalOffset -= scrollAmountForGamepadLeftRight;
                PressGamepadButtonAndVerifyOffsetAndFocus(GamepadButton.LeftThumbstickLeft, "Button 1C", expectedHorizontalOffset, expectedVerticalOffset);

                //Left. Change focus. Scroll. 
                expectedHorizontalOffset = 80;
                PressGamepadButtonAndVerifyOffsetAndFocus(GamepadButton.LeftThumbstickLeft, "Button 1B", expectedHorizontalOffset, expectedVerticalOffset);
            }
        }

        [TestMethod]
        [TestProperty("Description", "Tests GamePad interaction")]
        public void VerifyScrollerViewGamePadTriggerInteraction()
        {
            if (PlatformConfiguration.IsOSVersionLessThan(OSVersion.Redstone4))
            {
                Log.Warning("Test is disabled on pre-RS4 because ScrollerView Gamepad interaction is not supported pre-RS4");
                return;
            }
            
            using (IDisposable setup = new TestSetupHelper("ScrollerView Tests"),
                               setup2 = new TestSetupHelper("navigateToSimpleContents"))
            {
                UIObject buttonInScrollerView11;
                Scroller scroller11;
                SetupSimpleSingleScrollerViewTest(out buttonInScrollerView11, out scroller11);

                var scrollAmountForGamepadTrigger = scroller11.BoundingRectangle.Height;

                double expectedVerticalOffset = 0;
                double expectedHorizontalOffset = 0;

                //Down. Change focus. Scroll.
                expectedVerticalOffset = 220;
                PressGamepadButtonAndVerifyOffsetAndFocus(GamepadButton.RightTrigger, "Button 3", expectedHorizontalOffset, expectedVerticalOffset);

                //Down. Don't change focus. Scroll.
                expectedVerticalOffset += scrollAmountForGamepadTrigger;
                PressGamepadButtonAndVerifyOffsetAndFocus(GamepadButton.RightTrigger, "Button 3", expectedHorizontalOffset, expectedVerticalOffset);

                //Up. Don't change focus. Scroll.
                expectedVerticalOffset -= scrollAmountForGamepadTrigger;
                PressGamepadButtonAndVerifyOffsetAndFocus(GamepadButton.LeftTrigger, "Button 3", expectedHorizontalOffset, expectedVerticalOffset);

                //Up. Change focus. Scroll.
                expectedVerticalOffset = 0;
                PressGamepadButtonAndVerifyOffsetAndFocus(GamepadButton.LeftTrigger, "Button 1", expectedHorizontalOffset, expectedVerticalOffset);
            }
        }

        [TestMethod]
        [TestProperty("Description", "Tests GamePad interaction")]
        public void VerifyScrollerViewGamePadBumperInteraction()
        {
            if (PlatformConfiguration.IsOSVersionLessThan(OSVersion.Redstone4))
            {
                Log.Warning("Test is disabled on pre-RS4 because ScrollerView Gamepad interaction is not supported pre-RS4");
                return;
            }
            
            using (IDisposable setup = new TestSetupHelper("ScrollerView Tests"),
                               setup2 = new TestSetupHelper("navigateToSimpleContents"))
            {
                UIObject buttonInScrollerView11;
                Scroller scroller11;
                SetupSimpleSingleScrollerViewTest(out buttonInScrollerView11, out scroller11);

                var scrollAmountForBumper = scroller11.BoundingRectangle.Width;

                double expectedVerticalOffset = 0;
                double expectedHorizontalOffset = 0;

                //Right. Change focus. Scroll.
                expectedHorizontalOffset = 320;
                PressGamepadButtonAndVerifyOffsetAndFocus(GamepadButton.RightShoulder, "Button 1C", expectedHorizontalOffset, expectedVerticalOffset);

                //Right. Don't change focus. Scroll.
                expectedHorizontalOffset += scrollAmountForBumper;
                PressGamepadButtonAndVerifyOffsetAndFocus(GamepadButton.RightShoulder, "Button 1C", expectedHorizontalOffset, expectedVerticalOffset);

                //Left. Don't change focus. Scroll.
                expectedHorizontalOffset -= scrollAmountForBumper;
                PressGamepadButtonAndVerifyOffsetAndFocus(GamepadButton.LeftShoulder, "Button 1C", expectedHorizontalOffset, expectedVerticalOffset);

                //Left. Change focus. Scroll.
                expectedHorizontalOffset = 80;
                PressGamepadButtonAndVerifyOffsetAndFocus(GamepadButton.LeftShoulder, "Button 1B", expectedHorizontalOffset, expectedVerticalOffset);
            }
        }

        private void SetupSimpleSingleScrollerViewTest(out UIObject buttonInScrollerView11, out Scroller scroller11)
        {
            Log.Comment("Retrieving cmbShowScrollerView");
            ComboBox cmbShowScrollerView = new ComboBox(FindElement.ByName("cmbShowScrollerView"));
            Verify.IsNotNull(cmbShowScrollerView, "Verifying that cmbShowScrollerView was found");

            Log.Comment("Changing ScrollerView selection to scrollerView11");
            cmbShowScrollerView.SelectItemByName("scrollerView_11");
            Log.Comment("Selection is now {0}", cmbShowScrollerView.Selection[0].Name);

            Log.Comment("Retrieving buttonInScrollerView11");
            buttonInScrollerView11 = FindElement.ById("buttonInScrollerView11");
            Verify.IsNotNull(buttonInScrollerView11, "Verifying that buttonInScrollerView11 was found");

            Log.Comment("Retrieving scroller11");
            scroller11 = new Scroller(buttonInScrollerView11.Parent);
            Verify.IsNotNull(scroller11, "Verifying that scroller11 was found");

            WaitForScrollerViewFinalSize(scroller11, 300.0 /*expectedWidth*/, 400.0 /*expectedHeight*/);

            buttonInScrollerView11.Click();
            Wait.ForIdle();
        }

        private void SetupScrollerViewTestWithImage(string suffix, out UIObject imageInScrollerView, out Scroller scroller)
        {
            Log.Comment("Retrieving cmbShowScrollerView");
            ComboBox cmbShowScrollerView = new ComboBox(FindElement.ByName("cmbShowScrollerView"));
            Verify.IsNotNull(cmbShowScrollerView, "Verifying that cmbShowScrollerView was found");

            Log.Comment("Changing ScrollerView selection to scrollerView" + suffix);
            cmbShowScrollerView.SelectItemByName("scrollerView_" + suffix);
            Log.Comment("Selection is now {0}", cmbShowScrollerView.Selection[0].Name);

            Log.Comment("Retrieving img" + suffix);
            imageInScrollerView = FindElement.ById("img" + suffix);
            Verify.IsNotNull(imageInScrollerView, "Verifying that img" + suffix + " was found");

            Log.Comment("Retrieving Scroller");
            scroller = new Scroller(imageInScrollerView.Parent);
            Verify.IsNotNull(scroller, "Verifying that Scroller was found");

            WaitForScrollerViewFinalSize(scroller, 300.0 /*expectedWidth*/, 400.0 /*expectedHeight*/);

            imageInScrollerView.Click();
            Wait.ForIdle();
        }

        private void PressGamepadButtonAndVerifyOffsetAndFocus(GamepadButton gamepadButton, string expectedFocusedItemName, double expectedHorizontalOffset, double expectedVerticalOffset)
        {
            var focusChangedWaiter = new FocusAcquiredWaiter();
            bool waitForFocusChange = UIObject.Focused.Name != expectedFocusedItemName;

            GamepadHelper.PressButton(null, gamepadButton);

            if (waitForFocusChange)
            {
                focusChangedWaiter.Wait(TimeSpan.FromSeconds(2));
            }

            WaitForScrollerViewOffsets(expectedHorizontalOffset, expectedVerticalOffset);
            Verify.AreEqual(expectedFocusedItemName, UIObject.Focused.Name, "Verify focused element");
        }

        private void WaitForScrollerViewOffsets(double expectedHorizontalOffset, double expectedVerticalOffset)
        {
            Log.Comment("Waiting for ScrollerView offsets: {0}, {1}", expectedHorizontalOffset, expectedVerticalOffset);

            double actualHorizontalOffset;
            double actualVerticalOffset;
            float actualZoomFactor;
            GetScrollerView(out actualHorizontalOffset, out actualVerticalOffset, out actualZoomFactor);

            Func<bool> areOffsetsCorrect = () => AreClose(expectedHorizontalOffset, actualHorizontalOffset) && AreClose(expectedVerticalOffset, actualVerticalOffset);

            int triesRemaining = 10;
            while(!areOffsetsCorrect() && triesRemaining-- > 0)
            {
                Thread.Sleep(500);
                GetScrollerView(out actualHorizontalOffset, out actualVerticalOffset, out actualZoomFactor);
            }

            Verify.IsTrue(areOffsetsCorrect(), String.Format("Verify ScrollerView offsets. Expected = {0},{1}, Actual={2},{3}.",
                    expectedHorizontalOffset, expectedVerticalOffset, actualHorizontalOffset, actualVerticalOffset));
        }

        private bool AreClose(double expected, double actual, double delta = 0.1)
        {
            return Math.Abs(expected - actual) <= delta;
        }

        private void VerifyAreClose(double expected, double actual, double delta, string message)
        {
            Verify.IsTrue(AreClose(expected, actual, delta), String.Format("{0}, expected={1}, actual={2}, delta={3}", message, expected, actual, delta));
        }

        private void TapResetViewsButton()
        {
            Log.Comment("Retrieving btnResetViews");
            UIObject resetViewsUIObject = FindElement.ByName("btnResetViews");
            Verify.IsNotNull(resetViewsUIObject, "Verifying that btnResetViews Button was found");

            Button resetViewsButton = new Button(resetViewsUIObject);
            InputHelper.Tap(resetViewsButton);
            if (!WaitForEditValue("txtResetStatus" /*editName*/, "Views reset" /*editValue*/, 4.0 /*secondsTimeout*/, false /*throwOnError*/))
            {
                InputHelper.Tap(resetViewsButton);
                WaitForEditValue("txtResetStatus" /*editName*/, "Views reset" /*editValue*/, 4.0 /*secondsTimeout*/, false /*throwOnError*/);
            }
        }

        private void GetScrollerView(out double horizontalOffset, out double verticalOffset, out float zoomFactor)
        {
            horizontalOffset = 0.0;
            verticalOffset = 0.0;
            zoomFactor = 1.0f;

            UIObject viewUIObject = FindElement.ById("txtScrollerHorizontalOffset");
            Edit viewTextBox = new Edit(viewUIObject);
            Log.Comment("Current HorizontalOffset: " + viewTextBox.Value);
            horizontalOffset = String.IsNullOrWhiteSpace(viewTextBox.Value) ? double.NaN : Convert.ToDouble(viewTextBox.Value);

            viewUIObject = FindElement.ById("txtScrollerVerticalOffset");
            viewTextBox = new Edit(viewUIObject);
            Log.Comment("Current VerticalOffset: " + viewTextBox.Value);
            verticalOffset = String.IsNullOrWhiteSpace(viewTextBox.Value) ? double.NaN : Convert.ToDouble(viewTextBox.Value);

            viewUIObject = FindElement.ById("txtScrollerZoomFactor");
            viewTextBox = new Edit(viewUIObject);
            Log.Comment("Current ZoomFactor: " + viewTextBox.Value);
            zoomFactor = String.IsNullOrWhiteSpace(viewTextBox.Value) ? float.NaN : Convert.ToSingle(viewTextBox.Value);
        }

        private void PrepareForScrollerViewManipulationStart(string stateTextBoxName = "txtScrollerState")
        {
            UIObject scrollerStateUIObject = FindElement.ById(stateTextBoxName);
            Edit scrollerStateTextBox = new Edit(scrollerStateUIObject);
            Log.Comment("Pre-manipulation ScrollerState: " + scrollerStateTextBox.Value);
            Wait.ForIdle();
        }

        private bool WaitForEditValue(string editName, string editValue, double secondsTimeout = 2.0, bool throwOnError = true)
        {
            Edit edit = new Edit(FindElement.ById(editName));
            Verify.IsNotNull(edit);
            Log.Comment("Current value for " + editName + ": " + edit.Value);
            if (edit.Value != editValue)
            {
                using (var waiter = new ValueChangedEventWaiter(edit, editValue))
                {
                    Log.Comment("Waiting for " + editName + " to be set to " + editValue);

                    bool success = waiter.TryWait(TimeSpan.FromSeconds(secondsTimeout));
                    Log.Comment("Current value for " + editName + ": " + edit.Value);

                    if (success)
                    {
                        Log.Comment("Wait succeeded");
                    }
                    else
                    {
                        if (edit.Value == editValue)
                        {
                            Log.Warning("Wait failed but TextBox contains expected Text");
                            LogAndClearTraces();
                        }
                        else
                        {
                            if (throwOnError)
                            {
                                Log.Error("Wait for edit value failed");
                                LogAndClearTraces();
                                throw new WaiterException();
                            }
                            else
                            {
                                Log.Warning("Wait for edit value failed");
                                LogAndClearTraces();
                                return false;
                            }
                        }
                    }
                }
            }
            return true;
        }

        private void WaitForScrollerViewManipulationEnd(string scrollerViewName, string stateTextBoxName = "txtScrollerState")
        {
            WaitForManipulationEnd(scrollerViewName, stateTextBoxName);
        }

        private bool TryWaitForScrollerViewManipulationEnd(string scrollerViewName, string stateTextBoxName = "txtScrollerState")
        {
            return WaitForManipulationEnd(scrollerViewName, stateTextBoxName, false /*throwOnError*/);
        }

        private bool WaitForManipulationEnd(string elementName, string stateTextBoxName, bool throwOnError = true)
        {
            UIObject elementStateUIObject = FindElement.ById(stateTextBoxName);
            Edit elementStateTextBox = new Edit(elementStateUIObject);
            Log.Comment("Current State: " + elementStateTextBox.Value);
            if (elementStateTextBox.Value != elementName + ".PART_Root.PART_Scroller Idle")
            {
                using (var waiter = new ValueChangedEventWaiter(elementStateTextBox, elementName + ".PART_Root.PART_Scroller Idle"))
                {
                    int loops = 0;

                    Log.Comment("Waiting for " + elementName + "'s manipulation end.");
                    while (true)
                    {
                        bool success = waiter.TryWait(TimeSpan.FromMilliseconds(250));

                        Log.Comment("Current State: " + elementStateTextBox.Value);

                        if (success)
                        {
                            Log.Comment("Wait succeeded");
                            break;
                        }
                        else
                        {
                            if (elementStateTextBox.Value == elementName + ".PART_Root.PART_Scroller Idle")
                            {
                                Log.Warning("Wait failed but TextBox contains expected text");
                                LogAndClearTraces();
                                break;
                            }
                            else if (loops < 20)
                            {
                                loops++;
                                waiter.Reset();
                            }
                            else
                            {
                                if (throwOnError)
                                {
                                    Log.Error("Wait for manipulation end failed");
                                    LogAndClearTraces();
                                    throw new WaiterException();
                                }
                                else
                                {
                                    Log.Warning("Wait for manipulation end failed");
                                    LogAndClearTraces();
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }

        private void WaitForScrollerViewFinalSize(UIObject scrollerViewUIObject, double expectedWidth, double expectedHeight)
        {
            int pauses = 0;
            int widthDelta = Math.Abs(scrollerViewUIObject.BoundingRectangle.Width - (int)expectedWidth);
            int heightDelta = Math.Abs(scrollerViewUIObject.BoundingRectangle.Height - (int)expectedHeight);

            Log.Comment("scrollerViewUIObject.BoundingRectangle={0}", scrollerViewUIObject.BoundingRectangle);

            while (widthDelta > 1 || heightDelta > 1 && pauses < 5)
            {
                Wait.ForMilliseconds(60);
                pauses++;
                Log.Comment("scrollerViewUIObject.BoundingRectangle={0}", scrollerViewUIObject.BoundingRectangle);
                widthDelta = Math.Abs(scrollerViewUIObject.BoundingRectangle.Width - (int)expectedWidth);
                heightDelta = Math.Abs(scrollerViewUIObject.BoundingRectangle.Height - (int)expectedHeight);
            };

            Verify.IsLessThanOrEqual(widthDelta, 1);
            Verify.IsLessThanOrEqual(heightDelta, 1);
        }

        private void LogTraces()
        {
            Log.Comment("Reading full log:");

            UIObject fullLogUIObject = FindElement.ById("cmbFullLog");
            Verify.IsNotNull(fullLogUIObject);
            ComboBox cmbFullLog = new ComboBox(fullLogUIObject);
            Verify.IsNotNull(cmbFullLog);

            UIObject getFullLogUIObject = FindElement.ById("btnGetFullLog");
            Verify.IsNotNull(getFullLogUIObject);
            Button getFullLogButton = new Button(getFullLogUIObject);
            Verify.IsNotNull(getFullLogButton);

            getFullLogButton.Invoke();
            Wait.ForIdle();

            foreach (ComboBoxItem item in cmbFullLog.AllItems)
            {
                Log.Comment(item.Name);
            }
        }

        private void ClearTraces()
        {
            Log.Comment("Clearing full log.");

            UIObject clearFullLogUIObject = FindElement.ById("btnClearFullLog");
            Verify.IsNotNull(clearFullLogUIObject);
            Button clearFullLogButton = new Button(clearFullLogUIObject);
            Verify.IsNotNull(clearFullLogButton);

            clearFullLogButton.Invoke();
            Wait.ForIdle();
        }

        private void LogAndClearTraces()
        {
            LogTraces();
            ClearTraces();
        }
    }
}
