using System;
using System.Collections.Generic;
using System.Linq;
using RSG;
using Unity.UIWidgets.animation;
using Unity.UIWidgets.foundation;
using Unity.UIWidgets.gestures;
using Unity.UIWidgets.painting;
using Unity.UIWidgets.rendering;
using Unity.UIWidgets.service;
using Unity.UIWidgets.ui;
using Unity.UIWidgets.widgets;
using UnityEngine;
using Canvas = Unity.UIWidgets.ui.Canvas;
using Color = Unity.UIWidgets.ui.Color;
using Rect = Unity.UIWidgets.ui.Rect;
using TextStyle = Unity.UIWidgets.painting.TextStyle;

namespace Unity.UIWidgets.material {
    class DropdownConstants {
        public static readonly TimeSpan _kDropdownMenuDuration = new TimeSpan(0, 0, 0, 0, 300);
        public const float _kMenuItemHeight = 48.0f;
        public const float _kDenseButtonHeight = 24.0f;
        public static readonly EdgeInsets _kMenuItemPadding = EdgeInsets.symmetric(horizontal: 16.0f);
        public static readonly EdgeInsets _kAlignedButtonPadding = EdgeInsets.only(left: 16.0f, right: 4.0f);
        public static readonly EdgeInsets _kUnalignedButtonPadding = EdgeInsets.zero;
        public static readonly EdgeInsets _kAlignedMenuMargin = EdgeInsets.zero;
        public static readonly EdgeInsets _kUnalignedMenuMargin = EdgeInsets.only(left: 16.0f, right: 24.0f);
    }

    class _DropdownMenuPainter : AbstractCustomPainter {
        public _DropdownMenuPainter(
            Color color = null,
            int? elevation = null,
            int? selectedIndex = null,
            Animation<float> resize = null
        ) : base(repaint: resize) {
            D.assert(elevation != null);
            _painter = new BoxDecoration(
                color: color,
                borderRadius: BorderRadius.circular(2.0f),
                boxShadow: ShadowConstants.kElevationToShadow[elevation ?? 0]
            ).createBoxPainter();
            this.color = color;
            this.elevation = elevation;
            this.selectedIndex = selectedIndex;
            this.resize = resize;
        }

        public readonly Color color;
        public readonly int? elevation;
        public readonly int? selectedIndex;
        public readonly Animation<float> resize;

        public readonly BoxPainter _painter;

        public override void paint(Canvas canvas, Size size) {
            float selectedItemOffset = selectedIndex ?? 0 * DropdownConstants._kMenuItemHeight +
                                       Constants.kMaterialListPadding.top;
            FloatTween top = new FloatTween(
                begin: selectedItemOffset.clamp(0.0f, size.height - DropdownConstants._kMenuItemHeight),
                end: 0.0f
            );

            FloatTween bottom = new FloatTween(
                begin: (top.begin + DropdownConstants._kMenuItemHeight).clamp(DropdownConstants._kMenuItemHeight,
                    size.height),
                end: size.height
            );

            Rect rect = Rect.fromLTRB(0.0f, top.evaluate(resize), size.width, bottom.evaluate(resize));

            _painter.paint(canvas, rect.topLeft, new ImageConfiguration(size: rect.size));
        }

        public override bool shouldRepaint(CustomPainter painter) {
            _DropdownMenuPainter oldPainter = painter as _DropdownMenuPainter;
            return oldPainter.color != color
                   || oldPainter.elevation != elevation
                   || oldPainter.selectedIndex != selectedIndex
                   || oldPainter.resize != resize;
        }
    }

    class _DropdownScrollBehavior : ScrollBehavior {
        public _DropdownScrollBehavior() {
        }

        public override Widget buildViewportChrome(BuildContext context, Widget child, AxisDirection axisDirection) {
            return child;
        }

        public override ScrollPhysics getScrollPhysics(BuildContext context) {
            return new ClampingScrollPhysics();
        }
    }

    class _DropdownMenu<T> : StatefulWidget where T : class {
        public _DropdownMenu(
            Key key = null,
            EdgeInsets padding = null,
            _DropdownRoute<T> route = null
        ) : base(key: key) {
            this.route = route;
            this.padding = padding;
        }

        public readonly _DropdownRoute<T> route;
        public readonly EdgeInsets padding;

        public override State createState() {
            return new _DropdownMenuState<T>();
        }
    }

    class _DropdownMenuState<T> : State<_DropdownMenu<T>> where T : class {
        CurvedAnimation _fadeOpacity;
        CurvedAnimation _resize;

        public _DropdownMenuState() {
        }

        public override void initState() {
            base.initState();
            _fadeOpacity = new CurvedAnimation(
                parent: widget.route.animation,
                curve: new Interval(0.0f, 0.25f),
                reverseCurve: new Interval(0.75f, 1.0f)
            );
            _resize = new CurvedAnimation(
                parent: widget.route.animation,
                curve: new Interval(0.25f, 0.5f),
                reverseCurve: new Threshold(0.0f)
            );
        }

        public override Widget build(BuildContext context) {
            D.assert(MaterialD.debugCheckHasMaterialLocalizations(context));
            MaterialLocalizations localizations = MaterialLocalizations.of(context);
            _DropdownRoute<T> route = widget.route;
            float unit = 0.5f / (route.items.Count + 1.5f);
            List<Widget> children = new List<Widget>();
            for (int itemIndex = 0; itemIndex < route.items.Count; ++itemIndex) {
                CurvedAnimation opacity;
                if (itemIndex == route.selectedIndex) {
                    opacity = new CurvedAnimation(parent: route.animation, curve: new Threshold(0.0f));
                }
                else {
                    float start = (0.5f + (itemIndex + 1) * unit).clamp(0.0f, 1.0f);
                    float end = (start + 1.5f * unit).clamp(0.0f, 1.0f);
                    opacity = new CurvedAnimation(parent: route.animation, curve: new Interval(start, end));
                }

                var index = itemIndex;
                children.Add(new FadeTransition(
                    opacity: opacity,
                    child: new InkWell(
                        child: new Container(
                            padding: widget.padding,
                            child: route.items[itemIndex]
                        ),
                        onTap: () => Navigator.pop(
                            context,
                            new _DropdownRouteResult<T>(route.items[index].value)
                        )
                    )
                ));
            }

            return new FadeTransition(
                opacity: _fadeOpacity,
                child: new CustomPaint(
                    painter: new _DropdownMenuPainter(
                        color: Theme.of(context).canvasColor,
                        elevation: route.elevation,
                        selectedIndex: route.selectedIndex,
                        resize: _resize
                    ),
                    child: new Material(
                        type: MaterialType.transparency,
                        textStyle: route.style,
                        child: new ScrollConfiguration(
                            behavior: new _DropdownScrollBehavior(),
                            child: new Scrollbar(
                                child: new ListView(
                                    controller: widget.route.scrollController,
                                    padding: Constants.kMaterialListPadding,
                                    itemExtent: DropdownConstants._kMenuItemHeight,
                                    shrinkWrap: true,
                                    children: children
                                )
                            )
                        )
                    )
                )
            );
        }
    }

    class _DropdownMenuRouteLayout<T> : SingleChildLayoutDelegate {
        public _DropdownMenuRouteLayout(
            Rect buttonRect,
            float menuTop,
            float menuHeight
        ) {
            this.buttonRect = buttonRect;
            this.menuTop = menuTop;
            this.menuHeight = menuHeight;
        }

        public readonly Rect buttonRect;
        public readonly float menuTop;
        public readonly float menuHeight;

        public override BoxConstraints getConstraintsForChild(BoxConstraints constraints) {
            float maxHeight = Mathf.Max(0.0f, constraints.maxHeight - 2 * DropdownConstants._kMenuItemHeight);
            float width = Mathf.Min(constraints.maxWidth, buttonRect.width);
            return new BoxConstraints(
                minWidth: width,
                maxWidth: width,
                minHeight: 0.0f,
                maxHeight: maxHeight
            );
        }

        public override Offset getPositionForChild(Size size, Size childSize) {
            D.assert(() => {
                Rect container = Offset.zero & size;
                if (container.intersect(buttonRect) == buttonRect) {
                    D.assert(menuTop >= 0.0f);
                    D.assert(menuTop + menuHeight <= size.height);
                }

                return true;
            });
            float left = buttonRect.right.clamp(0.0f, size.width) - childSize.width;
            return new Offset(left, menuTop);
        }

        public override bool shouldRelayout(SingleChildLayoutDelegate _oldDelegate) {
            _DropdownMenuRouteLayout<T> oldDelegate = _oldDelegate as _DropdownMenuRouteLayout<T>;
            return buttonRect != oldDelegate.buttonRect
                   || menuTop != oldDelegate.menuTop
                   || menuHeight != oldDelegate.menuHeight;
        }
    }

    class _DropdownRouteResult<T> where T: class {
        public _DropdownRouteResult(T result) {
            this.result = result;
        }

        public readonly T result;

        public static bool operator ==(_DropdownRouteResult<T> left, _DropdownRouteResult<T> right) {
            return left.Equals(right);
        }

        public static bool operator !=(_DropdownRouteResult<T> left, _DropdownRouteResult<T> right) {
            return !left.Equals(right);
        }

        public bool Equals(_DropdownRouteResult<T> other) {
            return result == other.result;
        }
        
        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            if (ReferenceEquals(this, obj)) {
                return true;
            }
            if (obj.GetType() != GetType()) {
                return false;
            }
            return Equals((_DropdownRouteResult<T>) obj);
        }

        public override int GetHashCode() {
            return result.GetHashCode();
        }
    }

    class _DropdownRoute<T> : PopupRoute where T : class {
        public _DropdownRoute(
            List<DropdownMenuItem<T>> items = null,
            EdgeInsets padding = null,
            Rect buttonRect = null,
            int? selectedIndex = null,
            int elevation = 8,
            ThemeData theme = null,
            TextStyle style = null,
            string barrierLabel = null
        ) {
            D.assert(style != null);
            this.items = items;
            this.padding = padding;
            this.buttonRect = buttonRect;
            this.selectedIndex = selectedIndex;
            this.elevation = elevation;
            this.theme = theme;
            this.style = style;
            this.barrierLabel = barrierLabel;
        }

        public readonly List<DropdownMenuItem<T>> items;
        public readonly EdgeInsets padding;
        public readonly Rect buttonRect;
        public readonly int? selectedIndex;
        public readonly int elevation;
        public readonly ThemeData theme;
        public readonly TextStyle style;

        public ScrollController scrollController;

        public override TimeSpan transitionDuration {
            get { return DropdownConstants._kDropdownMenuDuration; }
        }

        public override bool barrierDismissible {
            get { return true; }
        }

        public override Color barrierColor {
            get { return null; }
        }

        public string barrierLabel;

        public override Widget buildPage(BuildContext context, Animation<float> animation,
            Animation<float> secondaryAnimation) {
            return new LayoutBuilder(
                builder: (ctx, constraints) => {
                    return new _DropdownRoutePage<T>(
                        route: this,
                        constraints: constraints,
                        items: items,
                        padding: padding,
                        buttonRect: buttonRect,
                        selectedIndex: selectedIndex,
                        elevation: elevation,
                        theme: theme,
                        style: style
                    );
                }
            );
        }

        internal void _dismiss() {
            navigator?.removeRoute(this);
        }
    }

    class _DropdownRoutePage<T> : StatelessWidget where T : class {
        public _DropdownRoutePage(
            Key key = null,
            _DropdownRoute<T> route = null,
            BoxConstraints constraints = null,
            List<DropdownMenuItem<T>> items = null,
            EdgeInsets padding = null,
            Rect buttonRect = null,
            int? selectedIndex = null,
            int elevation = 0,
            ThemeData theme = null,
            TextStyle style = null
        ) : base(key: key) {
            this.route = route;
            this.constraints = constraints;
            this.items = items;
            this.padding = padding;
            this.buttonRect = buttonRect;
            this.selectedIndex = selectedIndex;
            this.elevation = elevation;
            this.theme = theme;
            this.style = style;
        }

        public readonly _DropdownRoute<T> route;
        public readonly BoxConstraints constraints;
        public readonly List<DropdownMenuItem<T>> items;
        public readonly EdgeInsets padding;
        public readonly Rect buttonRect;
        public readonly int? selectedIndex;
        public readonly int elevation;
        public readonly ThemeData theme;
        public readonly TextStyle style;
        
        public override Widget build(BuildContext context) {
            D.assert(WidgetsD.debugCheckHasDirectionality(context));
            float availableHeight = constraints.maxHeight;
            float maxMenuHeight = availableHeight - 2.0f * DropdownConstants._kMenuItemHeight;

            float buttonTop = buttonRect.top;
            float buttonBottom = Mathf.Min(buttonRect.bottom, availableHeight);

            float topLimit = Mathf.Min(DropdownConstants._kMenuItemHeight, buttonTop);
            float bottomLimit = Mathf.Max(availableHeight - DropdownConstants._kMenuItemHeight, buttonBottom);

            float? selectedItemOffset = selectedIndex * DropdownConstants._kMenuItemHeight +
                                        Constants.kMaterialListPadding.top;

            float? menuTop = (buttonTop - selectedItemOffset) -
                             (DropdownConstants._kMenuItemHeight - buttonRect.height) / 2.0f;
            float preferredMenuHeight = (items.Count * DropdownConstants._kMenuItemHeight) +
                                        Constants.kMaterialListPadding.vertical;

            float menuHeight = Mathf.Min(maxMenuHeight, preferredMenuHeight);

            float? menuBottom = menuTop + menuHeight;

            if (menuTop < topLimit) {
                menuTop = Mathf.Min(buttonTop, topLimit);
            }

            if (menuBottom > bottomLimit) {
                menuBottom = Mathf.Max(buttonBottom, bottomLimit);
                menuTop = menuBottom - menuHeight;
            }

            if (route.scrollController == null) {
                float scrollOffset = preferredMenuHeight > maxMenuHeight
                    ? Mathf.Max(0.0f, selectedItemOffset ?? 0.0f - (buttonTop - (menuTop ?? 0.0f)))
                    : 0.0f;
                route.scrollController = new ScrollController(initialScrollOffset: scrollOffset);
            }

            Widget menu = new _DropdownMenu<T>(
                route: route,
                padding: padding
            );

            if (theme != null) {
                menu = new Theme(data: theme, child: menu);
            }

            return MediaQuery.removePadding(
                context: context,
                removeTop: true,
                removeBottom: true,
                removeLeft: true,
                removeRight: true,
                child: new Builder(
                    builder: (BuildContext _context) => {
                        return new CustomSingleChildLayout(
                            layoutDelegate: new _DropdownMenuRouteLayout<T>(
                                buttonRect: buttonRect,
                                menuTop: menuTop ?? 0.0f,
                                menuHeight: menuHeight
                            ),
                            child: menu
                        );
                    }
                )
            );
        }
    }

    public class DropdownMenuItem<T> : StatelessWidget where T : class {
        public DropdownMenuItem(
            Key key = null,
            T value = null,
            Widget child = null
        ) : base(key: key) {
            D.assert(child != null);
            this.value = value;
            this.child = child;
        }

        public readonly Widget child;

        public readonly T value;

        public override Widget build(BuildContext context) {
            return new Container(
                height: DropdownConstants._kMenuItemHeight,
                alignment: Alignment.centerLeft,
                child: child
            );
        }
    }

    public class DropdownButtonHideUnderline : InheritedWidget {
        public DropdownButtonHideUnderline(
            Key key = null,
            Widget child = null
        ) : base(key: key, child: child) {
            D.assert(child != null);
        }

        public static bool at(BuildContext context) {
            return context.inheritFromWidgetOfExactType(typeof(DropdownButtonHideUnderline)) != null;
        }

        public override bool updateShouldNotify(InheritedWidget oldWidget) {
            return false;
        }
    }

    public class DropdownButton<T> : StatefulWidget where T : class {
        public DropdownButton(
            Key key = null,
            List<DropdownMenuItem<T>> items = null,
            T value = null,
            Widget hint = null,
            Widget disabledHint = null,
            ValueChanged<T> onChanged = null,
            int elevation = 8,
            TextStyle style = null,
            Widget underline = null,
            Widget icon = null,
            Color iconDisabledColor = null,
            Color iconEnabledColor = null,
            float iconSize = 24.0f,
            bool isDense = false,
            bool isExpanded = false
        ) :
            base(key: key) {
            D.assert(items == null || value == null ||
                     items.Where<DropdownMenuItem<T>>((DropdownMenuItem<T> item) => item.value.Equals(value)).ToList()
                         .Count == 1);
            this.items = items;
            this.value = value;
            this.hint = hint;
            this.disabledHint = disabledHint;
            this.onChanged = onChanged;
            this.elevation = elevation;
            this.style = style;
            this.underline = underline;
            this.icon = icon;
            this.iconDisabledColor = iconDisabledColor;
            this.iconEnabledColor = iconEnabledColor;
            this.iconSize = iconSize;
            this.isDense = isDense;
            this.isExpanded = isExpanded;
        }

        public readonly List<DropdownMenuItem<T>> items;

        public readonly T value;

        public readonly Widget hint;

        public readonly Widget disabledHint;

        public readonly ValueChanged<T> onChanged;

        public readonly int elevation;

        public readonly TextStyle style;

        public readonly Widget underline;
            
        public readonly Widget icon;
                    
        public readonly Color iconDisabledColor;
                    
        public readonly Color iconEnabledColor;

        public readonly float iconSize;

        public readonly bool isDense;

        public readonly bool isExpanded;

        public override State createState() {
            return new _DropdownButtonState<T>();
        }
    }

    class _DropdownButtonState<T> : State<DropdownButton<T>>, WidgetsBindingObserver where T : class {
        int? _selectedIndex;
        _DropdownRoute<T> _dropdownRoute;

        public void didChangeTextScaleFactor() {
        }

        public void didChangeLocales(List<Locale> locale) {
        }

        public IPromise<bool> didPopRoute() {
            return Promise<bool>.Resolved(false);
        }

        public IPromise<bool> didPushRoute(string route) {
            return Promise<bool>.Resolved(false);
        }

        public void didChangePlatformBrightness() {
        }

        public override void initState() {
            base.initState();
            _updateSelectedIndex();
            WidgetsBinding.instance.addObserver(this);
        }

        public override void dispose() {
            WidgetsBinding.instance.removeObserver(this);
            _removeDropdownRoute();
            base.dispose();
        }

        public void didChangeMetrics() {
            _removeDropdownRoute();
        }

        void _removeDropdownRoute() {
            _dropdownRoute?._dismiss();
            _dropdownRoute = null;
        }

        public override void didUpdateWidget(StatefulWidget oldWidget) {
            base.didUpdateWidget(oldWidget);
            _updateSelectedIndex();
        }

        void _updateSelectedIndex() {
            if (!_enabled) {
                return;
            }

            D.assert(widget.value == null ||
                     widget.items.Where((DropdownMenuItem<T> item) => item.value.Equals(widget.value))
                         .ToList().Count == 1);
            _selectedIndex = null;
            for (int itemIndex = 0; itemIndex < widget.items.Count; itemIndex++) {
                if (widget.items[itemIndex].value.Equals(widget.value)) {
                    _selectedIndex = itemIndex;
                    return;
                }
            }
        }

        TextStyle _textStyle {
            get { return widget.style ?? Theme.of(context).textTheme.subhead; }
        }

        void _handleTap() {
            RenderBox itemBox = (RenderBox) context.findRenderObject();
            Rect itemRect = itemBox.localToGlobal(Offset.zero) & itemBox.size;
            EdgeInsets menuMargin = ButtonTheme.of(context).alignedDropdown
                ? DropdownConstants._kAlignedMenuMargin
                : DropdownConstants._kUnalignedMenuMargin;

            D.assert(_dropdownRoute == null);
            _dropdownRoute = new _DropdownRoute<T>(
                items: widget.items,
                buttonRect: menuMargin.inflateRect(itemRect),
                padding: DropdownConstants._kMenuItemPadding,
                selectedIndex: _selectedIndex ?? 0,
                elevation: widget.elevation,
                theme: Theme.of(context, shadowThemeOnly: true),
                style: _textStyle,
                barrierLabel: MaterialLocalizations.of(context).modalBarrierDismissLabel
            );

            Navigator.push(context, _dropdownRoute).Then(newValue => {
                _DropdownRouteResult<T> value = newValue as _DropdownRouteResult<T>;
                _dropdownRoute = null;
                if (!mounted || newValue == null) {
                    return;
                }

                if (widget.onChanged != null) {
                    widget.onChanged(value.result);
                }
            });
        }

        float? _denseButtonHeight {
            get {
                return Mathf.Max(_textStyle.fontSize ?? 0.0f,
                    Mathf.Max(widget.iconSize, DropdownConstants._kDenseButtonHeight));
            }
        }

        Color _iconColor {
            get {
                if (_enabled) {
                    if (widget.iconEnabledColor != null) {
                        return widget.iconEnabledColor;
                    }

                    switch (Theme.of(context).brightness) {
                        case Brightness.light:
                            return Colors.grey.shade700;
                        case Brightness.dark:
                            return Colors.white70;
                    }
                }
                else {
                    if (widget.iconDisabledColor != null) {
                        return widget.iconDisabledColor;
                    }

                    switch (Theme.of(context).brightness) {
                        case Brightness.light:
                            return Colors.grey.shade400;
                        case Brightness.dark:
                            return Colors.white10;
                    }
                }
                D.assert(false);
                return null;
            }
        }

        bool _enabled {
            get { return widget.items != null && widget.items.isNotEmpty() && widget.onChanged != null; }
        }

        public override Widget build(BuildContext context) {
            D.assert(MaterialD.debugCheckHasMaterial(context));
            D.assert(MaterialD.debugCheckHasMaterialLocalizations(context));

            List<Widget> items = _enabled ? new List<Widget>(widget.items) : new List<Widget>();
            int hintIndex = 0;
            if (widget.hint != null || (!_enabled && widget.disabledHint != null)) {
                Widget emplacedHint =
                    _enabled
                        ? widget.hint
                        : new DropdownMenuItem<Widget>(child: widget.disabledHint ?? widget.hint);
                hintIndex = items.Count;
                items.Add(new DefaultTextStyle(
                    style: _textStyle.copyWith(color: Theme.of(context).hintColor),
                    child: new IgnorePointer(
                        child: emplacedHint
                    )
                ));
            }

            EdgeInsets padding = ButtonTheme.of(context).alignedDropdown
                ? DropdownConstants._kAlignedButtonPadding
                : DropdownConstants._kUnalignedButtonPadding;

            IndexedStack innerItemsWidget = new IndexedStack(
                index: _enabled ? (_selectedIndex ?? hintIndex) : hintIndex,
                alignment: Alignment.centerLeft,
                children: items
            );

            Icon defaultIcon = new Icon(Icons.arrow_drop_down);
            Widget result = new DefaultTextStyle(
                style: _textStyle,
                child: new Container(
                    padding: padding,
                    height: widget.isDense ? _denseButtonHeight : null,
                    child: new Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        mainAxisSize: MainAxisSize.min,
                        children: new List<Widget> {
                            widget.isExpanded ? new Expanded(child: innerItemsWidget) : (Widget) innerItemsWidget,
                            new IconTheme(
                                data: new IconThemeData(
                                    color: _iconColor,
                                    size: widget.iconSize
                                ),
                                child: widget.icon ?? defaultIcon
                            )
                        }
                    )
                )
            );

            if (!DropdownButtonHideUnderline.at(context)) {
                float bottom = widget.isDense ? 0.0f : 8.0f;
                result = new Stack(
                    children: new List<Widget> {
                        result,
                        new Positioned(
                            left: 0.0f,
                            right: 0.0f,
                            bottom: bottom,
                            child: widget.underline ?? new Container(
                                height: 1.0f,
                                decoration: new BoxDecoration(
                                    border: new Border(
                                        bottom: new BorderSide(color: new Color(0xFFBDBDBD), width: 0.0f))
                                )
                            )
                        )
                    }
                );
            }

            return new GestureDetector(
                onTap: _enabled ? (GestureTapCallback) _handleTap : null,
                behavior: HitTestBehavior.opaque,
                child: result
            );
        }
    }

    public class DropdownButtonFormField<T> : FormField<T> where T : class {
        public DropdownButtonFormField(
            Key key = null,
            T value = null,
            List<DropdownMenuItem<T>> items = null,
            ValueChanged<T> onChanged = null,
            InputDecoration decoration = null,
            FormFieldSetter<T> onSaved = null,
            FormFieldValidator<T> validator = null,
            Widget hint = null
        ) : base(
            key: key,
            onSaved: onSaved,
            initialValue: value,
            validator: validator,
            builder: (FormFieldState<T> field) => {
                InputDecoration effectiveDecoration = (decoration ?? new InputDecoration())
                    .applyDefaults(Theme.of(field.context).inputDecorationTheme);
                return new InputDecorator(
                    decoration: effectiveDecoration.copyWith(errorText: field.errorText),
                    isEmpty: value == null,
                    child: new DropdownButtonHideUnderline(
                        child: new DropdownButton<T>(
                            isDense: true,
                            value: value,
                            items: items,
                            hint: hint,
                            onChanged: field.didChange
                        )
                    )
                );
            }
        ) {
            this.onChanged = onChanged;
        }

        public readonly ValueChanged<T> onChanged;

        public override State createState() {
            return new _DropdownButtonFormFieldState<T>();
        }
    }

    class _DropdownButtonFormFieldState<T> : FormFieldState<T> where T : class {
        public new DropdownButtonFormField<T> widget {
            get { return base.widget as DropdownButtonFormField<T>; }
        }

        public override void didChange(T value) {
            base.didChange(value);
            if (widget.onChanged != null) {
                widget.onChanged(value);
            }
        }
    }
}