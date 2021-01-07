namespace Unity.UIWidgets.foundation {
    public class DiagnosticableMixinChangeNotifier : ChangeNotifier, IDiagnosticable {
        protected DiagnosticableMixinChangeNotifier() {
        }

        public virtual string toStringShort() {
            return foundation_.describeIdentity(this);
        }

        public override string ToString() {
            return toString();
        }

        public virtual string toString(DiagnosticLevel minLevel = DiagnosticLevel.debug) {
            string fullString = null;
            D.assert(() => {
                fullString = toDiagnosticsNode(style: DiagnosticsTreeStyle.singleLine)
                    .toString(minLevel: minLevel);
                return true;
            });
            return fullString ?? toStringShort();
        }

        public virtual DiagnosticsNode toDiagnosticsNode(
            string name = null,
            DiagnosticsTreeStyle style = DiagnosticsTreeStyle.sparse) {
            return new DiagnosticableNode<DiagnosticableMixinChangeNotifier>(
                name: name, value: this, style: style
            );
        }

        public virtual void debugFillProperties(DiagnosticPropertiesBuilder properties) {
        }
    }

}

