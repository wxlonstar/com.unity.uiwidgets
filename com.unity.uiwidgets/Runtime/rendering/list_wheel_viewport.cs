using System;
using System.Collections.Generic;
using Unity.UIWidgets.animation;
using Unity.UIWidgets.foundation;
using Unity.UIWidgets.gestures;
using Unity.UIWidgets.painting;
using Unity.UIWidgets.ui;
using UnityEngine;
using Rect = Unity.UIWidgets.ui.Rect;

namespace Unity.UIWidgets.rendering {
    delegate float ___ChildSizingFunction(RenderBox child);

    public interface IListWheelChildManager {
        int? childCount { get; }
        bool childExistsAt(int index);
        void createChild(int index, RenderBox after);
        void removeChild(RenderBox child);
    }

    public class ListWheelParentData : ContainerBoxParentData<RenderBox> {
        public int index;
    }

    public class RenderListWheelViewport : ContainerRenderObjectMixinRenderBox<RenderBox, ListWheelParentData>,
        RenderAbstractViewport {
        public RenderListWheelViewport(
            IListWheelChildManager childManager,
            ViewportOffset offset,
            float itemExtent,
            float diameterRatio = defaultDiameterRatio,
            float perspective = defaultPerspective,
            float offAxisFraction = 0.0f,
            bool useMagnifier = false,
            float magnification = 1.0f,
            bool clipToSize = true,
            bool renderChildrenOutsideViewport = false,
            List<RenderBox> children = null
        ) {
            D.assert(childManager != null);
            D.assert(offset != null);
            D.assert(diameterRatio > 0, () => diameterRatioZeroMessage);
            D.assert(perspective > 0);
            D.assert(perspective <= 0.01f, () => perspectiveTooHighMessage);
            D.assert(magnification > 0);
            D.assert(itemExtent > 0);
            D.assert(
                !renderChildrenOutsideViewport || !clipToSize,
                () => clipToSizeAndRenderChildrenOutsideViewportConflict
            );

            this.childManager = childManager;
            _offset = offset;
            _diameterRatio = diameterRatio;
            _perspective = perspective;
            _offAxisFraction = offAxisFraction;
            _useMagnifier = useMagnifier;
            _magnification = magnification;
            _itemExtent = itemExtent;
            _clipToSize = clipToSize;
            _renderChildrenOutsideViewport = renderChildrenOutsideViewport;
            addAll(children);
        }

        public const float defaultDiameterRatio = 2.0f;

        public const float defaultPerspective = 0.003f;

        public const string diameterRatioZeroMessage = "You can't set a diameterRatio " +
                                                       "of 0 or of a negative number. It would imply a cylinder of 0 in diameter " +
                                                       "in which case nothing will be drawn.";

        public const string perspectiveTooHighMessage = "A perspective too high will " +
                                                        "be clipped in the z-axis and therefore not renderable. Value must be " +
                                                        "between 0 and 0.0f1.";

        public const string clipToSizeAndRenderChildrenOutsideViewportConflict =
            "Cannot renderChildrenOutsideViewport and clipToSize since children " +
            "rendered outside will be clipped anyway.";

        public readonly IListWheelChildManager childManager;

        public ViewportOffset offset {
            get { return _offset; }
            set {
                D.assert(value != null);
                if (value == _offset) {
                    return;
                }

                if (attached) {
                    _offset.removeListener(_hasScrolled);
                }

                _offset = value;
                if (attached) {
                    _offset.addListener(_hasScrolled);
                }

                markNeedsLayout();
            }
        }

        ViewportOffset _offset;

        public float diameterRatio {
            get { return _diameterRatio; }
            set {
                D.assert(
                    value > 0,
                    () => diameterRatioZeroMessage
                );

                _diameterRatio = value;
                markNeedsPaint();
            }
        }

        float _diameterRatio;

        public float perspective {
            get { return _perspective; }
            set {
                D.assert(value > 0);
                D.assert(
                    value <= 0.01f,
                    () => perspectiveTooHighMessage
                );
                if (value == _perspective) {
                    return;
                }

                _perspective = value;
                markNeedsPaint();
            }
        }

        float _perspective;

        public float offAxisFraction {
            get { return _offAxisFraction; }
            set {
                if (value == _offAxisFraction) {
                    return;
                }

                _offAxisFraction = value;
                markNeedsPaint();
            }
        }

        float _offAxisFraction = 0.0f;

        public bool useMagnifier {
            get { return _useMagnifier; }
            set {
                if (value == _useMagnifier) {
                    return;
                }

                _useMagnifier = value;
                markNeedsPaint();
            }
        }

        bool _useMagnifier = false;

        public float magnification {
            get { return _magnification; }
            set {
                D.assert(value > 0);
                if (value == _magnification) {
                    return;
                }

                _magnification = value;
                markNeedsPaint();
            }
        }

        float _magnification = 1.0f;

        public float itemExtent {
            get { return _itemExtent; }
            set {
                D.assert(value > 0);
                if (value == _itemExtent) {
                    return;
                }

                _itemExtent = value;
                markNeedsLayout();
            }
        }

        float _itemExtent;

        public bool clipToSize {
            get { return _clipToSize; }
            set {
                D.assert(
                    !renderChildrenOutsideViewport || !clipToSize,
                    () => clipToSizeAndRenderChildrenOutsideViewportConflict
                );
                if (value == _clipToSize) {
                    return;
                }

                _clipToSize = value;
                markNeedsPaint();
            }
        }

        bool _clipToSize;

        public bool renderChildrenOutsideViewport {
            get { return _renderChildrenOutsideViewport; }
            set {
                D.assert(
                    !renderChildrenOutsideViewport || !clipToSize,
                    () => clipToSizeAndRenderChildrenOutsideViewportConflict
                );
                if (value == _renderChildrenOutsideViewport) {
                    return;
                }

                _renderChildrenOutsideViewport = value;
                markNeedsLayout();
            }
        }

        bool _renderChildrenOutsideViewport;


        void _hasScrolled() {
            markNeedsLayout();
        }

        public override void setupParentData(RenderObject child) {
            if (!(child.parentData is ListWheelParentData)) {
                child.parentData = new ListWheelParentData();
            }
        }

        public override void attach(object owner) {
            base.attach(owner);
            _offset.addListener(_hasScrolled);
        }

        public override void detach() {
            _offset.removeListener(_hasScrolled);
            base.detach();
        }

        public override bool isRepaintBoundary {
            get { return true; }
        }

        float _viewportExtent {
            get {
                D.assert(hasSize);
                return size.height;
            }
        }

        float _minEstimatedScrollExtent {
            get {
                D.assert(hasSize);
                if (childManager.childCount == null) {
                    return float.NegativeInfinity;
                }

                return 0.0f;
            }
        }

        float _maxEstimatedScrollExtent {
            get {
                D.assert(hasSize);
                if (childManager.childCount == null) {
                    return float.PositiveInfinity;
                }

                return Mathf.Max(0.0f, ((childManager.childCount ?? 0) - 1) * _itemExtent);
            }
        }

        float _topScrollMarginExtent {
            get {
                D.assert(hasSize);
                return -size.height / 2.0f + _itemExtent / 2.0f;
            }
        }

        float _getUntransformedPaintingCoordinateY(float layoutCoordinateY) {
            return layoutCoordinateY - _topScrollMarginExtent - offset.pixels;
        }

        float _maxVisibleRadian {
            get {
                if (_diameterRatio < 1.0f) {
                    return Mathf.PI / 2.0f;
                }

                return Mathf.Asin(1.0f / _diameterRatio);
            }
        }

        float _getIntrinsicCrossAxis(___ChildSizingFunction childSize) {
            float extent = 0.0f;
            RenderBox child = firstChild;
            while (child != null) {
                extent = Mathf.Max(extent, childSize(child));
                child = childAfter(child);
            }

            return extent;
        }

        protected override float computeMinIntrinsicWidth(float height) {
            return _getIntrinsicCrossAxis(
                (RenderBox child) => child.getMinIntrinsicWidth(height)
            );
        }

        protected override float computeMaxIntrinsicWidth(float height) {
            return _getIntrinsicCrossAxis(
                (RenderBox child) => child.getMaxIntrinsicWidth(height)
            );
        }

        protected override float computeMinIntrinsicHeight(float width) {
            if (childManager.childCount == null) {
                return 0.0f;
            }

            return (childManager.childCount ?? 0) * _itemExtent;
        }

        protected internal override float computeMaxIntrinsicHeight(float width) {
            if (childManager.childCount == null) {
                return 0.0f;
            }

            return (childManager.childCount ?? 0) * _itemExtent;
        }

        protected override bool sizedByParent {
            get { return true; }
        }

        protected override void performResize() {
            size = constraints.biggest;
        }

        public int indexOf(RenderBox child) {
            D.assert(child != null);
            ListWheelParentData childParentData = (ListWheelParentData) child.parentData;
            return childParentData.index;
        }

        public int scrollOffsetToIndex(float scrollOffset) {
            return (scrollOffset / itemExtent).floor();
        }

        public float indexToScrollOffset(int index) {
            return index * itemExtent;
        }

        void _createChild(int index,
            RenderBox after = null
        ) {
            invokeLayoutCallback<BoxConstraints>((BoxConstraints constraints) => {
                D.assert(this.constraints == this.constraints);
                childManager.createChild(index, after: after);
            });
        }

        void _destroyChild(RenderBox child) {
            invokeLayoutCallback<BoxConstraints>((BoxConstraints constraints) => {
                D.assert(this.constraints == this.constraints);
                childManager.removeChild(child);
            });
        }

        void _layoutChild(RenderBox child, BoxConstraints constraints, int index) {
            child.layout(constraints, parentUsesSize: true);
            ListWheelParentData childParentData = (ListWheelParentData) child.parentData;
            float crossPosition = size.width / 2.0f - child.size.width / 2.0f;
            childParentData.offset = new Offset(crossPosition, indexToScrollOffset(index));
        }

        protected override void performLayout() {
            BoxConstraints childConstraints = constraints.copyWith(
                minHeight: _itemExtent,
                maxHeight: _itemExtent,
                minWidth: 0.0f
            );

            float visibleHeight = size.height;
            if (renderChildrenOutsideViewport) {
                visibleHeight *= 2;
            }

            float firstVisibleOffset = offset.pixels + _itemExtent / 2 - visibleHeight / 2;
            float lastVisibleOffset = firstVisibleOffset + visibleHeight;

            int targetFirstIndex = scrollOffsetToIndex(firstVisibleOffset);
            int targetLastIndex = scrollOffsetToIndex(lastVisibleOffset);

            if (targetLastIndex * _itemExtent == lastVisibleOffset) {
                targetLastIndex--;
            }

            while (!childManager.childExistsAt(targetFirstIndex) && targetFirstIndex <= targetLastIndex) {
                targetFirstIndex++;
            }

            while (!childManager.childExistsAt(targetLastIndex) && targetFirstIndex <= targetLastIndex) {
                targetLastIndex--;
            }

            if (targetFirstIndex > targetLastIndex) {
                while (firstChild != null) {
                    _destroyChild(firstChild);
                }

                return;
            }


            if (childCount > 0 &&
                (indexOf(firstChild) > targetLastIndex || indexOf(lastChild) < targetFirstIndex)) {
                while (firstChild != null) {
                    _destroyChild(firstChild);
                }
            }


            if (childCount == 0) {
                _createChild(targetFirstIndex);
                _layoutChild(firstChild, childConstraints, targetFirstIndex);
            }

            int currentFirstIndex = indexOf(firstChild);
            int currentLastIndex = indexOf(lastChild);

            while (currentFirstIndex < targetFirstIndex) {
                _destroyChild(firstChild);
                currentFirstIndex++;
            }

            while (currentLastIndex > targetLastIndex) {
                _destroyChild(lastChild);
                currentLastIndex--;
            }

            RenderBox child = firstChild;
            while (child != null) {
                child.layout(childConstraints, parentUsesSize: true);
                child = childAfter(child);
            }

            while (currentFirstIndex > targetFirstIndex) {
                _createChild(currentFirstIndex - 1);
                _layoutChild(firstChild, childConstraints, --currentFirstIndex);
            }

            while (currentLastIndex < targetLastIndex) {
                _createChild(currentLastIndex + 1, after: lastChild);
                _layoutChild(lastChild, childConstraints, ++currentLastIndex);
            }

            offset.applyViewportDimension(_viewportExtent);

            float minScrollExtent = childManager.childExistsAt(targetFirstIndex - 1)
                ? _minEstimatedScrollExtent
                : indexToScrollOffset(targetFirstIndex);
            float maxScrollExtent = childManager.childExistsAt(targetLastIndex + 1)
                ? _maxEstimatedScrollExtent
                : indexToScrollOffset(targetLastIndex);
            offset.applyContentDimensions(minScrollExtent, maxScrollExtent);
        }

        bool _shouldClipAtCurrentOffset() {
            float highestUntransformedPaintY = _getUntransformedPaintingCoordinateY(0.0f);
            return highestUntransformedPaintY < 0.0f
                   || size.height < highestUntransformedPaintY + _maxEstimatedScrollExtent + _itemExtent;
        }

        public override void paint(PaintingContext context, Offset offset) {
            if (childCount > 0) {
                if (_clipToSize && _shouldClipAtCurrentOffset()) {
                    context.pushClipRect(
                        needsCompositing,
                        offset,
                        Offset.zero & size, _paintVisibleChildren
                    );
                }
                else {
                    _paintVisibleChildren(context, offset);
                }
            }
        }

        void _paintVisibleChildren(PaintingContext context, Offset offset) {
            RenderBox childToPaint = firstChild;
            ListWheelParentData childParentData = (ListWheelParentData) childToPaint?.parentData;

            while (childParentData != null) {
                _paintTransformedChild(childToPaint, context, offset, childParentData.offset);
                childToPaint = childAfter(childToPaint);
                childParentData = (ListWheelParentData) childToPaint?.parentData;
            }
        }

        void _paintTransformedChild(RenderBox child, PaintingContext context, Offset offset, Offset layoutOffset) {
            Offset untransformedPaintingCoordinates = offset + new Offset(
                                                          layoutOffset.dx,
                                                          _getUntransformedPaintingCoordinateY(layoutOffset.dy)
                                                      );


            float fractionalY = (untransformedPaintingCoordinates.dy + _itemExtent / 2.0f) / size.height;

            float angle = -(fractionalY - 0.5f) * 2.0f * _maxVisibleRadian;
            if (angle > Mathf.PI / 2.0f || angle < -Mathf.PI / 2.0f) {
                return;
            }

            var radius = size.height * _diameterRatio / 2.0f;
            var deltaY = radius * Mathf.Sin(angle);

            Matrix3 transform = Matrix3.I();
            // Matrix4x4 transform2 = MatrixUtils.createCylindricalProjectionTransform(
            //     radius: this.size.height * this._diameterRatio / 2.0f,
            //     angle: angle,
            //     perspective: this._perspective
            // );

            // Offset offsetToCenter = new Offset(untransformedPaintingCoordinates.dx, -this._topScrollMarginExtent);

            Offset offsetToCenter =
                new Offset(untransformedPaintingCoordinates.dx, -deltaY - _topScrollMarginExtent);

            if (!useMagnifier) {
                _paintChildCylindrically(context, offset, child, transform, offsetToCenter);
            }
            else {
                _paintChildWithMagnifier(
                    context,
                    offset,
                    child,
                    transform,
                    offsetToCenter,
                    untransformedPaintingCoordinates
                );
            }
        }

        void _paintChildWithMagnifier(
            PaintingContext context,
            Offset offset,
            RenderBox child,
            // Matrix4x4 cylindricalTransform,
            Matrix3 cylindricalTransform,
            Offset offsetToCenter,
            Offset untransformedPaintingCoordinates
        ) {
            float magnifierTopLinePosition = size.height / 2 - _itemExtent * _magnification / 2;
            float magnifierBottomLinePosition = size.height / 2 + _itemExtent * _magnification / 2;

            bool isAfterMagnifierTopLine = untransformedPaintingCoordinates.dy
                                           >= magnifierTopLinePosition - _itemExtent * _magnification;
            bool isBeforeMagnifierBottomLine = untransformedPaintingCoordinates.dy
                                               <= magnifierBottomLinePosition;

            if (isAfterMagnifierTopLine && isBeforeMagnifierBottomLine) {
                Rect centerRect = Rect.fromLTWH(
                    0.0f,
                    magnifierTopLinePosition, size.width, _itemExtent * _magnification);
                Rect topHalfRect = Rect.fromLTWH(
                    0.0f,
                    0.0f, size.width,
                    magnifierTopLinePosition);
                Rect bottomHalfRect = Rect.fromLTWH(
                    0.0f,
                    magnifierBottomLinePosition, size.width,
                    magnifierTopLinePosition);

                context.pushClipRect(
                    false,
                    offset,
                    centerRect,
                    (PaintingContext context1, Offset offset1) => {
                        context1.pushTransform(
                            false,
                            offset1,
                            cylindricalTransform,
                            // this._centerOriginTransform(cylindricalTransform),
                            (PaintingContext context2, Offset offset2) => {
                                context2.paintChild(
                                    child,
                                    offset2 + untransformedPaintingCoordinates);
                            });
                    });

                context.pushClipRect(
                    false,
                    offset,
                    untransformedPaintingCoordinates.dy <= magnifierTopLinePosition
                        ? topHalfRect
                        : bottomHalfRect,
                    (PaintingContext context1, Offset offset1) => {
                        _paintChildCylindrically(
                            context1,
                            offset1,
                            child,
                            cylindricalTransform,
                            offsetToCenter
                        );
                    }
                );
            }
            else {
                _paintChildCylindrically(
                    context,
                    offset,
                    child,
                    cylindricalTransform,
                    offsetToCenter
                );
            }
        }

        void _paintChildCylindrically(
            PaintingContext context,
            Offset offset,
            RenderBox child,
            // Matrix4x4 cylindricalTransform,
            Matrix3 cylindricalTransform,
            Offset offsetToCenter
        ) {
            context.pushTransform(
                false,
                offset,
                cylindricalTransform,
                // this._centerOriginTransform(cylindricalTransform),
                (PaintingContext _context, Offset _offset) => { _context.paintChild(child, _offset + offsetToCenter); }
            );
        }

        public override Rect describeApproximatePaintClip(RenderObject child) {
            if (child != null && _shouldClipAtCurrentOffset()) {
                return Offset.zero & size;
            }

            return null;
        }

        protected override bool hitTestChildren(HitTestResult result, Offset position = null
        ) {
            return false;
        }

        public RevealedOffset getOffsetToReveal(RenderObject target, float alignment,
            Rect rect = null
        ) {
            rect = rect ?? target.paintBounds;

            RenderObject child = target;
            while (child.parent != this) {
                child = (RenderObject) child.parent;
            }

            ListWheelParentData parentData = (ListWheelParentData) child.parentData;
            float targetOffset = parentData.offset.dy;
            Matrix4x4 transform = target.getTransformTo(this).toMatrix4x4();
            Rect bounds = MatrixUtils.transformRect(transform, rect);
            Rect targetRect = bounds.translate(0.0f, (size.height - itemExtent) / 2);

            return new RevealedOffset(offset: targetOffset, rect: targetRect);
        }

        public new RenderObject parent {
            get { return (RenderObject) base.parent; }
        }

        public new void showOnScreen(
            RenderObject descendant = null,
            Rect rect = null,
            TimeSpan? duration = null,
            Curve curve = null
        ) {
            duration = duration ?? TimeSpan.Zero;
            curve = curve ?? Curves.ease;
            if (descendant != null) {
                RevealedOffset revealedOffset = getOffsetToReveal(descendant, 0.5f, rect: rect);
                if (duration == TimeSpan.Zero) {
                    offset.jumpTo(revealedOffset.offset);
                }
                else {
                    offset.animateTo(revealedOffset.offset, duration: (TimeSpan) duration, curve: curve);
                }

                rect = revealedOffset.rect;
            }

            base.showOnScreen(
                rect: rect,
                duration: duration,
                curve: curve
            );
        }
    }
}