namespace Estreya.BlishHUD.Shared.UI.Views.Controls;

using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

internal class ListViewProvider<T> : ControlProvider<List<T>, List<T>>
{
    private const int BUTTON_HEIGHT = 30;

    public override Control CreateControl(BoxedValue<List<T>> value, Func<List<T>, bool> isEnabled, Func<List<T>, bool> isValid, (float Min, float Max)? range, int width, int height, int x, int y)
    {
        Panel mainPanel = new Panel()
        {
            Location = new Point(x, y),
            Width = width,
            Height = 350
        };

        FlowPanel flowPanel = new FlowPanel()
        {
            Parent = mainPanel,
            Location = new Point(0, 0),
            Width = mainPanel.Width,
            Height = mainPanel.Height - BUTTON_HEIGHT,
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            CanScroll = true,
            ControlPadding = new Vector2(0, 20)
        };

        FlowPanel buttonPanel = new FlowPanel()
        {
            Parent = mainPanel,
            Location = new Point(flowPanel.Left, flowPanel.Bottom),
            Height = BUTTON_HEIGHT,
            Width = mainPanel.Width,
            FlowDirection = ControlFlowDirection.SingleRightToLeft
        };

        StandardButton addButton = new StandardButton()
        {
            Text = "Add",
            Parent = buttonPanel,
        };

        addButton.Click += (s, e) =>
        {
            ListViewControl<T> listViewControl = this.GetListViewControl(flowPanel, width, default);
            listViewControl.DeleteRequested += (s, e) =>
            {
                _ = value.Value.Remove(listViewControl.Control);
                _ = flowPanel.RemoveChild(listViewControl);
            };

            value.Value.Add(listViewControl.Control);
        };

        value.Value.ForEach(item =>
        {
            ListViewControl<T> listViewControl = this.GetListViewControl(flowPanel, width, item);
            listViewControl.DeleteRequested += (s, e) =>
            {
                _ = value.Value.Remove(listViewControl.Control);
                _ = flowPanel.RemoveChild(listViewControl);
            };
        });

        return mainPanel;
    }

    private ListViewControl<T> GetListViewControl(FlowPanel parent, int width, T value)
    {
        value ??= (T)Activator.CreateInstance(typeof(T));

        return new ListViewControl<T>(value, width)
        {
            Parent = parent,
            WidthSizingMode = SizingMode.Fill,
            HeightSizingMode = SizingMode.AutoSize,
            ShowBorder = true,
        };
    }

    private class ListViewControl<TCtrl> : Panel
    {
        private const int LABEL_WIDTH = 150;

        public TCtrl Control { get; }

        public event EventHandler DeleteRequested;

        private static MethodInfo _lambdaFunction;

        private static MethodInfo LambdaFunction
        {
            get
            {
                if (_lambdaFunction == null)
                {
                    IEnumerable<MethodInfo> methods = typeof(Expression).GetMethods().Where(method => method.IsGenericMethodDefinition &&
                        Enumerable.SequenceEqual(method.GetParameters().Select(p => p.Name), new string[] { "body", "parameters" }));

                    _lambdaFunction = methods.First();
                }

                return _lambdaFunction;
            }
        }

        public ListViewControl(TCtrl control, int width)
        {
            this.Control = control;

            System.Reflection.PropertyInfo[] properties = this.Control.GetType().GetProperties(System.Reflection.BindingFlags.Public| System.Reflection.BindingFlags.Instance);

            int y = 0;

            foreach (System.Reflection.PropertyInfo property in properties.OrderBy(prop => prop.DeclaringType == typeof(TCtrl) ? 1 : 0))
            {
                if (!property.CanWrite)
                {
                    continue;
                }

                if (property.GetCustomAttribute<TypeIgnoreAttribute>() != null)
                {
                    continue;
                }

                TypeOverrideAttribute typeOverrideAttribute = property.GetCustomAttribute<TypeOverrideAttribute>();

                ParameterExpression par = Expression.Parameter(typeof(TCtrl), "x");

                MemberExpression col = Expression.Property(par, property.Name);

                Type func = typeof(Func<,>).MakeGenericType(typeof(TCtrl), property.PropertyType);

                Expression lambda = (Expression)LambdaFunction
                    .MakeGenericMethod(func)
                    .Invoke(null, new object[] { col, new ParameterExpression[] { par } });

                try
                {
                    Label label = new Label()
                    {
                        Text = property.Name,
                        Parent = this,
                        Location = new Point(0, y),
                        Width = LABEL_WIDTH,
                        WrapText = true
                    };

                    Control ctrl;

                    if (typeOverrideAttribute != null)
                    {
                        ctrl = (Control)typeof(ControlHandler)
                                    .GetMethod(nameof(ControlHandler.CreateFromPropertyWithChangedTypeValidation))
                                    .MakeGenericMethod(typeof(TCtrl), property.PropertyType, typeOverrideAttribute.Type)
                                    .Invoke(null, new object[] { control, lambda, new Func<TCtrl, bool>(ctrl => true), null, null, width - LABEL_WIDTH - 40, -1, LABEL_WIDTH, y });
                    }
                    else
                    {

                        ctrl = (Control)typeof(ControlHandler)
                                    .GetMethod(nameof(ControlHandler.CreateFromProperty))
                                    .MakeGenericMethod(typeof(TCtrl), property.PropertyType)
                                    .Invoke(null, new object[] { control, lambda, new Func<TCtrl, bool>(ctrl => true), null, null, width - LABEL_WIDTH - 40, -1, LABEL_WIDTH, y });
                    }

                    ctrl.Parent = this;

                    y += Math.Max(ctrl.Height, label.Height) + 5;
                }
                catch (Exception ex)
                {
                    // Logger.Warn
                }
            }

            StandardButton deleteButton = new StandardButton()
            {
                Text = "Delete",
                Location = new Point(0, y),
                Width = width - 40,
                Parent = this
            };

            deleteButton.Click += (s, e) => this.DeleteRequested?.Invoke(deleteButton, EventArgs.Empty);
        }
    }
}

