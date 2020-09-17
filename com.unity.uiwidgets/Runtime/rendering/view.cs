﻿using Unity.UIWidgets.foundation;
using Unity.UIWidgets.gestures;
using Unity.UIWidgets.ui;

namespace Unity.UIWidgets.rendering {
    public class ViewConfiguration {
        public ViewConfiguration(
            Size size = null,
            float devicePixelRatio = 1.0f
        ) {
            this.size = size ?? Size.zero;
            this.devicePixelRatio = devicePixelRatio;
        }

        public readonly Size size;

        public readonly float devicePixelRatio;

        public Matrix3 toMatrix() {
            return Matrix3.I();
        }

        public override string ToString() {
            return $"${size} at ${devicePixelRatio}x";
        }
    }

    public class RenderView : RenderObjectWithChildMixinRenderObject<RenderBox> {
        public RenderView(
            RenderBox child = null,
            ViewConfiguration configuration = null) {
            D.assert(configuration != null);

            this.child = child;
            _configuration = configuration;
        }

        public Size size {
            get { return _size; }
        }

        Size _size = Size.zero;

        public ViewConfiguration configuration {
            get { return _configuration; }
            set {
                D.assert(value != null);
                if (value == _configuration) {
                    return;
                }

                _configuration = value;
                replaceRootLayer((OffsetLayer) _updateMatricesAndCreateNewRootLayer());
                markNeedsLayout();
            }
        }

        ViewConfiguration _configuration;

        public void scheduleInitialFrame() {
            D.assert(owner != null);
            scheduleInitialLayout();
            scheduleInitialPaint((OffsetLayer) _updateMatricesAndCreateNewRootLayer());
            owner.requestVisualUpdate();
        }

        Matrix3 _rootTransform;

        public Layer _updateMatricesAndCreateNewRootLayer() {
            _rootTransform = configuration.toMatrix();
            ContainerLayer rootLayer = new TransformLayer(transform: _rootTransform);
            rootLayer.attach(this);
            return rootLayer;
        }

        protected override void debugAssertDoesMeetConstraints() {
            D.assert(false);
        }

        protected override void performResize() {
            D.assert(false);
        }

        protected override void performLayout() {
            _size = configuration.size;
            D.assert(_size.isFinite);

            if (child != null) {
                child.layout(BoxConstraints.tight(_size));
            }
        }

        public bool hitTest(HitTestResult result, Offset position = null) {
            if (child != null) {
                child.hitTest(result, position: position);
            }

            result.add(new HitTestEntry(this));
            return true;
        }

        public override bool isRepaintBoundary {
            get { return true; }
        }

        public override void paint(PaintingContext context, Offset offset) {
            if (child != null) {
                context.paintChild(child, offset);
            }
        }

        public override void applyPaintTransform(RenderObject child, Matrix3 transform) {
            transform.preConcat(_rootTransform);
            base.applyPaintTransform(child, transform);
        }

        public void compositeFrame() {
            var builder = new SceneBuilder();
            using (var scene = layer.buildScene(builder)) {
                Window.instance.render(scene);
            }

            D.assert(() => {
                if (D.debugRepaintRainbowEnabled || D.debugRepaintTextRainbowEnabled) {
                    D.debugCurrentRepaintColor = D.debugCurrentRepaintColor.withHue((D.debugCurrentRepaintColor.hue + 2.0f) % 360.0f);
                }

                return true;
            });
        }

        public override Rect paintBounds {
            get { return Offset.zero & (size * configuration.devicePixelRatio); }
        }

        public override Rect semanticBounds {
            get {
                D.assert(_rootTransform != null);
                return _rootTransform.mapRect(Offset.zero & size);
            }
        }

        public override void debugFillProperties(DiagnosticPropertiesBuilder properties) {
            D.assert(() => {
                properties.add(DiagnosticsNode.message("debug mode enabled"));
                return true;
            });
            properties.add(new DiagnosticsProperty<Size>("window size", Window.instance.physicalSize,
                tooltip: "in physical pixels"));
            properties.add(new FloatProperty("device pixel ratio", Window.instance.devicePixelRatio,
                tooltip: "physical pixels per logical pixel"));
            properties.add(new DiagnosticsProperty<ViewConfiguration>("configuration", configuration,
                tooltip: "in logical pixels"));
        }
    }
}