namespace Estreya.BlishHUD.EventTable.UI.Views;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.EventTable.Models;
using Estreya.BlishHUD.Shared.Controls;
using Estreya.BlishHUD.Shared.Extensions;
using Estreya.BlishHUD.Shared.Services;
using Estreya.BlishHUD.Shared.Threading.Events;
using Estreya.BlishHUD.Shared.UI.Views;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

public class EditDynamicEventView : BaseView
{
    private Map _selectedMap;
    private List<Map> _maps;
    private DynamicEvent _dynamicEvent;

    private bool _isRemoveVisibile;

    public event AsyncEventHandler<DynamicEvent> SaveClicked;
    public event AsyncEventHandler<DynamicEvent> RemoveClicked;
    public event EventHandler CloseRequested;

    public EditDynamicEventView(DynamicEvent dynamicEvent, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, BitmapFont font = null) : base(apiManager, iconService, translationService, font)
    {
        this._dynamicEvent = dynamicEvent;
        this._isRemoveVisibile = this._dynamicEvent is not null && this._dynamicEvent.IsCustom;
    }

    private Vector3 GetCurrentPosition()
    {
        if (this._selectedMap is null) return Vector3.Zero;

        Vector2 v2 = this._selectedMap.WorldMeterCoordsToMapCoords(GameService.Gw2Mumble.PlayerCharacter.Position);
        return new Vector3(v2.X, v2.Y, GameService.Gw2Mumble.PlayerCharacter.Position.Z.ToInches());

    }

    protected override void InternalBuild(Panel parent)
    {
        int currentMapId = GameService.Gw2Mumble.CurrentMap.Id;
        this._selectedMap ??= this._maps.FirstOrDefault(m => m.Id == currentMapId);
        Vector3 currentPosition = this.GetCurrentPosition();
        this._dynamicEvent ??= new DynamicEvent()
        {
            Name = string.Empty,
            IsCustom = true,
            ID = Guid.NewGuid().ToString(),
            MapId = currentMapId,
            Location = new DynamicEvent.DynamicEventLocation()
            {
                Points = new float[][] { new float[] { currentPosition.X, currentPosition.Y, 0 } }, // Z = 0 is default center height
                Center = new[] { currentPosition.X, currentPosition.Y, currentPosition.Z },
                ZRange = new float[] { -50, 50 }
            }
        };

        parent.ClearChildren();

        FlowPanel flowPanel = new FlowPanel()
        {
            Parent = parent,
            Width = parent.ContentRegion.Width,
            CanScroll = true,
            Height = parent.ContentRegion.Height - (int)(Button.STANDARD_CONTROL_HEIGHT * 1.5),
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            OuterControlPadding = new Vector2(20, 20),
            ControlPadding = new Vector2(0, 10)
        };

        TextBox nameTextBox = this.RenderTextbox(flowPanel, Point.Zero, flowPanel.ContentRegion.Width - ((int)flowPanel.OuterControlPadding.X * 2), this._dynamicEvent.Name, "Name",
            val =>
            {
                this._dynamicEvent.Name = val;
            });
        nameTextBox.BasicTooltipText = "Defines the name of the dynamic event. It is shown in the tooltip on the (mini)map.";

        Panel mapSelectionGroup = new Panel()
        {
            Parent = flowPanel,
            HeightSizingMode = SizingMode.AutoSize,
            Width = flowPanel.ContentRegion.Width - ((int)flowPanel.OuterControlPadding.X * 2),
        };

        Label mapSelectionLabel = this.RenderLabel(mapSelectionGroup, "Map").TitleLabel;
        mapSelectionLabel.Width = this.LABEL_WIDTH;

        Shared.Controls.Dropdown<string> mapSelectionDropdown = this.RenderDropdown(
            mapSelectionGroup,
            new Point(this.LABEL_WIDTH, 0),
            mapSelectionGroup.ContentRegion.Width - this.LABEL_WIDTH,
            this._maps.OrderBy(m => m.Name).Select(this.GetMapDisplayName).ToArray(),
            this._selectedMap is null ? null : this.GetMapDisplayName(this._selectedMap),
            newMapDisplayName =>
            {
                string idString = new Regex(@".*?\( ID: (\d+) \)").Match(newMapDisplayName).Groups[1].Value;
                if (int.TryParse(idString, out int id))
                {
                    this._selectedMap = this._maps.FirstOrDefault(m => m.Id == id);
                }

                this.InternalBuild(parent);
            });
        mapSelectionDropdown.Enabled = false;
        mapSelectionDropdown.PanelHeight = 200;

        Panel typeSelectionGroup = new Panel()
        {
            Parent = flowPanel,
            HeightSizingMode = SizingMode.AutoSize,
            Width = flowPanel.ContentRegion.Width - ((int)flowPanel.OuterControlPadding.X * 2),
        };
        Label typeSelectionLabel = this.RenderLabel(typeSelectionGroup, "Type").TitleLabel;
        typeSelectionLabel.Width = this.LABEL_WIDTH;

        Shared.Controls.Dropdown<string> typeSelectionDropdown = this.RenderDropdown(
            typeSelectionGroup,
            new Point(this.LABEL_WIDTH, 0),
            typeSelectionGroup.ContentRegion.Width - this.LABEL_WIDTH,
            new string[] { "sphere", "cylinder", "poly" },
            this._dynamicEvent.Location.Type,
            newType => this._dynamicEvent.Location.Type = newType);
        typeSelectionDropdown.PanelHeight = 200;
        typeSelectionDropdown.BasicTooltipText = "Defines the type of the type of the dynamic event.";

        this.RenderLevelGroup(flowPanel, this._dynamicEvent);
        this.RenderColorGroup(flowPanel, this._dynamicEvent);

        FlowPanel typePropertiesPanel = new FlowPanel()
        {
            Parent = flowPanel,
            HeightSizingMode = SizingMode.AutoSize,
            Width = flowPanel.ContentRegion.Width - ((int)flowPanel.OuterControlPadding.X * 2),
        };

        this.RenderTypeProperties(typePropertiesPanel, this._dynamicEvent);

        typeSelectionDropdown.ValueChanged += (s, e) =>
        {
            this.RenderTypeProperties(typePropertiesPanel, this._dynamicEvent);
        };

        Func<Task> saveAction = async () =>
        {
            await (this.SaveClicked?.Invoke(this, this._dynamicEvent) ?? Task.CompletedTask);
        };

        Func<Task> saveAndCloseAction = async () =>
        {
            await saveAction();
            this.CloseRequested?.Invoke(this, EventArgs.Empty);
        };

        Func<Task> removeAction = async () =>
        {
            await (this.RemoveClicked?.Invoke(this, this._dynamicEvent) ?? Task.CompletedTask);
            this.CloseRequested?.Invoke(this, EventArgs.Empty);
        };

        Button saveButton = this.RenderButtonAsync(parent, "Save", saveAction);
        saveButton.Location = new Point(parent.Right - saveButton.Width - (int)flowPanel.OuterControlPadding.X, parent.Bottom - saveButton.Height);

        Button saveAndCloseButton = this.RenderButtonAsync(parent, "Save & Close", saveAndCloseAction);
        saveAndCloseButton.Right = saveButton.Left - 2;
        saveAndCloseButton.Top = saveButton.Top;

        if (this._isRemoveVisibile)
        {
            Button removeButton = this.RenderButtonAsync(parent, "Remove", removeAction);
            removeButton.Location = new Point(parent.Left + (int)flowPanel.OuterControlPadding.X, saveAndCloseButton.Top);
        }
    }

    private void RenderLevelGroup(FlowPanel parent, DynamicEvent dynamicEvent)
    {
        Panel levelGroup = new Panel()
        {
            Parent = parent,
            HeightSizingMode = SizingMode.AutoSize,
            Width = parent.ContentRegion.Width - ((int)parent.OuterControlPadding.X * 2),
        };
        Label levelLabel = this.RenderLabel(levelGroup, "Level").TitleLabel;
        levelLabel.Width = this.LABEL_WIDTH;

        var levelTextbox = this.RenderTextbox(levelGroup,
            new Point(this.LABEL_WIDTH, 0),
            levelGroup.ContentRegion.Width - this.LABEL_WIDTH,
            dynamicEvent.Level.ToString(), "Level",
            val =>
            {
                if (string.IsNullOrWhiteSpace(val)) val = "0";

                dynamicEvent.Level = int.Parse(val);
            },
            onBeforeChangeAction: (oldVal, newVal) =>
            {
                if (string.IsNullOrWhiteSpace(newVal)) return Task.FromResult(true);

               if (!int.TryParse(newVal, out int intVal))
                {
                    this.ShowError("Not a valid int value.");
                    return Task.FromResult(false);
                }

                if (intVal < 0)
                {
                    this.ShowError("Level can't be negative.");
                    return Task.FromResult(false);
                }

                if (intVal > 80)
                {
                    this.ShowError("Level can't be larger than 80.");
                    return Task.FromResult(false);
                }

                return Task.FromResult(true);
            });

        levelTextbox.BasicTooltipText = "Defines the recommended level of the dynamic event.";
    }

    private void RenderColorGroup(FlowPanel parent, DynamicEvent dynamicEvent)
    {
        Panel group = new Panel()
        {
            Parent = parent,
            HeightSizingMode = SizingMode.AutoSize,
            Width = parent.ContentRegion.Width - ((int)parent.OuterControlPadding.X * 2),
        };
        Label colorLabel = this.RenderLabel(group, "Color").TitleLabel;
        colorLabel.Width = this.LABEL_WIDTH;

       var colorTextbox = this.RenderTextbox(group,
            new Point(this.LABEL_WIDTH, 0),
            group.ContentRegion.Width - this.LABEL_WIDTH,
            dynamicEvent.ColorCode, "Color",
            val =>
            {
                dynamicEvent.ColorCode = val;
            });

        colorTextbox.BasicTooltipText = "Defines the color of the dynamic event. Empty = Default color";
    }

    private void RenderTypeProperties(FlowPanel parent, DynamicEvent dynamicEvent)
    {
        parent.ClearChildren();

        FlowPanel locationProperties = new FlowPanel()
        {
            Parent = parent,
            HeightSizingMode = SizingMode.AutoSize,
            Width = parent.ContentRegion.Width - ((int)parent.OuterControlPadding.X * 2),
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            Title = "Location",
            CanCollapse = true,
            OuterControlPadding = new Vector2(20, 20),
            ControlPadding = new Vector2(0, 2)
        };

        this.RenderLocationCenter(locationProperties, dynamicEvent);

        this.RenderEmptyLine(locationProperties, 20);

        switch (dynamicEvent.Location.Type?.ToLowerInvariant())
        {
            case "poly":
                this.RenderPolygoneProperties(locationProperties, dynamicEvent);
                break;
            case "cylinder":
                this.RenderCylinderProperties(locationProperties, dynamicEvent);
                break;
            case "sphere":
                this.RenderSphereProperties(locationProperties, dynamicEvent);
                break;
            default:
                break;
        }
    }

    private void RenderLocationCenter(FlowPanel parent, DynamicEvent dynamicEvent)
    {
        FlowPanel centerSectionPanel = new FlowPanel
        {
            Parent = parent,
            Width = parent.ContentRegion.Width - ((int)parent.OuterControlPadding.X * 2),
            HeightSizingMode = SizingMode.AutoSize,
            Title = "Center",
            OuterControlPadding = new Vector2(20, 20),
            ControlPadding = new Vector2(2, 0),
            CanCollapse = true,
            FlowDirection = ControlFlowDirection.SingleLeftToRight
        };

        TextBox xPos = this.RenderTextbox(centerSectionPanel, Point.Zero, 150, dynamicEvent.Location.Center[0].ToString(CultureInfo.CurrentCulture), "X Position", val =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(val)) val = "0";
                dynamicEvent.Location.Center[0] = float.Parse(val, CultureInfo.CurrentCulture);
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message);
            }
        });
        xPos.BasicTooltipText = "Defines the center position on the x-axis.";

        TextBox yPos = this.RenderTextbox(centerSectionPanel, Point.Zero, 150, dynamicEvent.Location.Center[1].ToString(CultureInfo.CurrentCulture), "Y Position", val =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(val)) val = "0";
                dynamicEvent.Location.Center[1] = float.Parse(val, CultureInfo.CurrentCulture);
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message);
            }
        });
        yPos.BasicTooltipText = "Defines the center position on the y-axis.";

        TextBox zPos = this.RenderTextbox(centerSectionPanel, Point.Zero, 150, dynamicEvent.Location.Center[2].ToString(CultureInfo.CurrentCulture), "Z Position", val =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(val)) val = "0";
                dynamicEvent.Location.Center[2] = float.Parse(val, CultureInfo.CurrentCulture);
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message);
            }
        });
        zPos.BasicTooltipText = "Defines the center position on the z-axis.";

        Button currentPositionButton = this.RenderButton(centerSectionPanel, "Set current Position", () =>
        {
            Vector3 currPos = this.GetCurrentPosition();

            xPos.Text = currPos.X.ToString(CultureInfo.CurrentCulture);
            yPos.Text = currPos.Y.ToString(CultureInfo.CurrentCulture);
            zPos.Text = currPos.Z.ToString(CultureInfo.CurrentCulture);
        });
        currentPositionButton.BasicTooltipText = "Sets the position values to the current position reported by mumblelink.";
    }

    private void RenderCylinderProperties(FlowPanel parent, DynamicEvent dynamicEvent)
    {
        if (dynamicEvent is null || dynamicEvent.Location.Type.ToLowerInvariant() != "cylinder") return;

        this.RenderHeightSection(parent, dynamicEvent);

        this.RenderRadiusSection(parent, dynamicEvent);
    }

    private void RenderSphereProperties(FlowPanel parent, DynamicEvent dynamicEvent)
    {
        if (dynamicEvent is null || dynamicEvent.Location.Type.ToLowerInvariant() != "sphere") return;

        this.RenderRadiusSection(parent, dynamicEvent);
    }

    private void RenderHeightSection(FlowPanel parent, DynamicEvent dynamicEvent)
    {
        FlowPanel heightPanelWrapper = new FlowPanel()
        {
            Parent = parent,
            HeightSizingMode = SizingMode.AutoSize,
            Width = parent.ContentRegion.Width - ((int)parent.OuterControlPadding.X * 2),
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            Title = "Height",
            CanCollapse = true,
            OuterControlPadding = new Vector2(20, 20),
            ControlPadding = new Vector2(0, 2)
        };

        Panel group = new Panel()
        {
            Parent = heightPanelWrapper,
            HeightSizingMode = SizingMode.AutoSize,
            Width = heightPanelWrapper.ContentRegion.Width - ((int)heightPanelWrapper.OuterControlPadding.X * 2),
        };

        Label heightLabel = this.RenderLabel(group, "Height").TitleLabel;
        heightLabel.Width = this.LABEL_WIDTH;

        var heightTextbox = this.RenderTextbox(group,
            new Point(this.LABEL_WIDTH, 0),
            group.ContentRegion.Width - this.LABEL_WIDTH,
            dynamicEvent.Location.Height.ToString(CultureInfo.CurrentCulture),
            "Height",
            val =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(val)) val = "0";
                    dynamicEvent.Location.Height = float.Parse(val, CultureInfo.CurrentCulture);
                }
                catch (Exception ex)
                {
                    this.ShowError(ex.Message);
                }
            });

        heightTextbox.BasicTooltipText = "Defines the height of the dynamic event in inches.";

        this.RenderEmptyLine(heightPanelWrapper, (int)heightPanelWrapper.OuterControlPadding.X);
    }
    private void RenderRadiusSection(FlowPanel parent, DynamicEvent dynamicEvent)
    {
        FlowPanel panelWrapper = new FlowPanel()
        {
            Parent = parent,
            HeightSizingMode = SizingMode.AutoSize,
            Width = parent.ContentRegion.Width - ((int)parent.OuterControlPadding.X * 2),
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            Title = "Radius",
            CanCollapse = true,
            OuterControlPadding = new Vector2(20, 20),
            ControlPadding = new Vector2(0, 2)
        };
        Panel group = new Panel()
        {
            Parent = panelWrapper,
            HeightSizingMode = SizingMode.AutoSize,
            Width = panelWrapper.ContentRegion.Width - ((int)panelWrapper.OuterControlPadding.X * 2),
        };

        Label label = this.RenderLabel(group, "Radius").TitleLabel;
        label.Width = this.LABEL_WIDTH;

        var radiusTextbox = this.RenderTextbox(group,
            new Point(this.LABEL_WIDTH, 0),
            group.ContentRegion.Width - this.LABEL_WIDTH,
            dynamicEvent.Location.Radius.ToString(CultureInfo.CurrentCulture),
            "Radius",
            val =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(val)) val = "0";
                    dynamicEvent.Location.Radius = float.Parse(val, CultureInfo.CurrentCulture);
                }
                catch (Exception ex)
                {
                    this.ShowError(ex.Message);
                }
            });

        radiusTextbox.BasicTooltipText = "Defines the radius of the dynamic event in inches.";

        this.RenderEmptyLine(panelWrapper, (int)panelWrapper.OuterControlPadding.X);
    }

    private void RenderPolygoneProperties(FlowPanel parent, DynamicEvent dynamicEvent)
    {
        if (dynamicEvent is null || dynamicEvent.Location.Type.ToLowerInvariant() != "poly") return;

        FlowPanel pointsPanelWrapper = new FlowPanel()
        {
            Parent = parent,
            HeightSizingMode = SizingMode.AutoSize,
            Width = parent.ContentRegion.Width - ((int)parent.OuterControlPadding.X * 2),
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            Title = "Points",
            CanCollapse = true,
            OuterControlPadding = new Vector2(20, 20),
            ControlPadding = new Vector2(0, 2)
        };

        FlowPanel pointsPanel = new FlowPanel()
        {
            Parent = pointsPanelWrapper,
            HeightSizingMode = SizingMode.AutoSize,
            Width = pointsPanelWrapper.ContentRegion.Width - ((int)pointsPanelWrapper.OuterControlPadding.X * 2),
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
        };

        this.RenderPolygonePoints(pointsPanel, dynamicEvent);

        this.RenderEmptyLine(pointsPanelWrapper, (int)pointsPanelWrapper.OuterControlPadding.Y);

        FlowPanel zRangePanel = new FlowPanel()
        {
            Parent = parent,
            HeightSizingMode = SizingMode.AutoSize,
            Width = parent.ContentRegion.Width - ((int)parent.OuterControlPadding.X * 2),
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            Title = "Z Range",
            CanCollapse = true,
            OuterControlPadding = new Vector2(20, 20),
            ControlPadding = new Vector2(0, 2)
        };

        this.RenderPolygoneZRange(zRangePanel, dynamicEvent);

        this.RenderEmptyLine(zRangePanel, 20);
    }

    private void RenderPolygoneZRange(FlowPanel parent, DynamicEvent dynamicEvent)
    {
        Panel topGroup = new Panel()
        {
            Parent = parent,
            HeightSizingMode = SizingMode.AutoSize,
            Width = parent.ContentRegion.Width - ((int)parent.OuterControlPadding.X * 2),
        };

        Label topRangeLabel = this.RenderLabel(topGroup, "Top").TitleLabel;
        topRangeLabel.Width = this.LABEL_WIDTH;

        TextBox topRangeTextbox = this.RenderTextbox(topGroup,
            new Point(this.LABEL_WIDTH, 0),
            topGroup.ContentRegion.Width - this.LABEL_WIDTH,
            dynamicEvent.Location.ZRange[1].ToString(CultureInfo.CurrentCulture),
            "Top Z Position",
            val =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(val)) val = "0";
                dynamicEvent.Location.ZRange[1] = float.Parse(val, CultureInfo.CurrentCulture);
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message);
            }
        });

        topRangeTextbox.BasicTooltipText = "Defines the top boundary of the z-axis. The vaule is a offset from the point z-axis.";

        Panel bottomGroup = new Panel()
        {
            Parent = parent,
            HeightSizingMode = SizingMode.AutoSize,
            Width = parent.ContentRegion.Width - ((int)parent.OuterControlPadding.X * 2),
        };

        Label bottomRangeLabel = this.RenderLabel(bottomGroup, "Bottom").TitleLabel;
        bottomRangeLabel.Width = this.LABEL_WIDTH;

        TextBox bottomRangeTextbox = this.RenderTextbox(bottomGroup,
            new Point(this.LABEL_WIDTH, 0),
            bottomGroup.ContentRegion.Width - this.LABEL_WIDTH,
            dynamicEvent.Location.ZRange[0].ToString(CultureInfo.CurrentCulture),
            "Bottom Z Position",
            val =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(val)) val = "0";
                dynamicEvent.Location.ZRange[0] = float.Parse(val, CultureInfo.CurrentCulture);
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message);
            }
        });

        bottomRangeTextbox.BasicTooltipText = "Defines the lower boundary of the z-axis. The vaule is a offset from the point z-axis.";
    }

    private void RenderPolygonePoints(FlowPanel parent, DynamicEvent dynamicEvent)
    {
        parent.ClearChildren();

        FlowPanel lastPointSection = null;

        if (dynamicEvent != null)
        {
            for (int i = 0; i < dynamicEvent.Location.Points.Length; i++)
            {
                if (dynamicEvent.Location.Points[i].Length < 3)
                {
                    Array.Resize(ref dynamicEvent.Location.Points[i], 3);
                }

                int index = i;
                lastPointSection = this.AddPolygonePointSection(parent, dynamicEvent.Location.Points[i]);

                Button upButton = this.RenderButton(lastPointSection, "U", () =>
                {
                    if (index <= 0) return;

                    dynamicEvent.Location.Points.MoveEntry(index, index - 1);
                    this.RenderPolygonePoints(parent, dynamicEvent);
                }, () => index == 0);
                upButton.Width = 30;
                upButton.BasicTooltipText = "Moves this point entry up in the list.";
                //upButton.Icon = this.IconService.GetIcon("155033.png");
                //upButton.ResizeIcon = true;
                //upButton.DrawBackground = false;

                Button downButton = this.RenderButton(lastPointSection, "D", () =>
                {
                    if (index > dynamicEvent.Location.Points.Length - 2) return;

                    dynamicEvent.Location.Points.MoveEntry(index, index + 1);
                    this.RenderPolygonePoints(parent, dynamicEvent);
                }, () => index == dynamicEvent.Location.Points.Length - 1);
                downButton.Width = 30;
                downButton.BasicTooltipText = "Moves this point entry down in the list.";
                //downButton.Icon = this.IconService.GetIcon("155034.png");
                //downButton.ResizeIcon = true;
                //downButton.DrawBackground = false;

                Control lastChild = lastPointSection.Children.Last();

                Button removeButton = this.RenderButton(lastPointSection, this.TranslationService.GetTranslation("editDynamicEventView-btn-remove", "Remove"), () =>
                {
                    List<float[]> newPoints = dynamicEvent.Location.Points.ToList();
                    newPoints.RemoveAt(index);
                    dynamicEvent.Location.Points = newPoints.ToArray();
                    this.RenderPolygonePoints(parent, dynamicEvent);
                }, () => dynamicEvent.Location.Points.Length == 1);

                removeButton.Left = lastChild.Right + 10;
                removeButton.Width = 120;
                removeButton.Icon = this.IconService.GetIcon("1444524.png");
                removeButton.ResizeIcon = false;
                removeButton.BasicTooltipText = "Removes the entry from the list.";
            }
        }

        int x = lastPointSection?.Children.LastOrDefault()?.Left ?? 0;

        Panel addButtonPanel = new Panel
        {
            Parent = parent,
            Width = x + 120,
            HeightSizingMode = SizingMode.AutoSize
        };

        Button addButton = this.RenderButton(addButtonPanel, this.TranslationService.GetTranslation("manageReminderTimesView-btn-add", "Add"), () =>
        {
            Vector3 currentPosition = this.GetCurrentPosition();
            dynamicEvent.Location.Points = new List<float[]>(dynamicEvent.Location.Points) {
                new[] { currentPosition.X, currentPosition.Y, 0 } // Z = 0 is default center height
            }.ToArray();
            this.RenderPolygonePoints(parent, dynamicEvent);
        });
        addButton.Left = x;
        addButton.Width = 120;
        addButton.Icon = this.IconService.GetIcon("1444520.png");
        addButton.ResizeIcon = false;
        addButton.BasicTooltipText = "Adds a new entry to the list.";
    }

    private FlowPanel AddPolygonePointSection(FlowPanel parent, float[] point)
    {
        FlowPanel positionSectionPanel = new FlowPanel
        {
            Parent = parent,
            Width = parent.ContentRegion.Width - ((int)parent.OuterControlPadding.X * 2),
            HeightSizingMode = SizingMode.AutoSize,
            ControlPadding = new Vector2(2, 0),
            FlowDirection = ControlFlowDirection.SingleLeftToRight
        };

        TextBox xPos = this.RenderTextbox(positionSectionPanel, Point.Zero, 150, point[0].ToString(CultureInfo.CurrentCulture), "X Position", val =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(val)) val = "0";
                point[0] = float.Parse(val, CultureInfo.CurrentCulture);
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message);
            }
        });
        xPos.BasicTooltipText = "Defines the position on the x-axis.";


        TextBox yPos = this.RenderTextbox(positionSectionPanel, new Point(xPos.Right + 5, 0), 150, point[1].ToString(CultureInfo.CurrentCulture), "Y Position", val =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(val)) val = "0";
                point[1] = float.Parse(val, CultureInfo.CurrentCulture);
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message);
            }
        });
        yPos.BasicTooltipText = "Defines the position on the y-axis.";

        TextBox zPos = this.RenderTextbox(positionSectionPanel, new Point(yPos.Right + 5, 0), 150, point[2].ToString(CultureInfo.CurrentCulture), "Z Position", val =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(val)) val = "0";
                point[2] = float.Parse(val, CultureInfo.CurrentCulture);
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message);
            }
        });
        zPos.BasicTooltipText = "Defines the position on the y-axis. A value of 0 uses the same value as the center.";

        var setCurrPosButton = this.RenderButton(positionSectionPanel, "Set current Position", () =>
        {
            Vector3 currPos = this.GetCurrentPosition();

            xPos.Text = currPos.X.ToString(CultureInfo.CurrentCulture);
            yPos.Text = currPos.Y.ToString(CultureInfo.CurrentCulture);
            zPos.Text = currPos.Z.ToString(CultureInfo.CurrentCulture);
        });
        setCurrPosButton.BasicTooltipText = "Sets the position values to the current position reported by mumblelink.";

        return positionSectionPanel;
    }

    private string GetMapDisplayName(Map map)
    {
        return $"{map.Name.Trim()} ( ID: {map.Id} )";
    }

    protected override async Task<bool> InternalLoad(IProgress<string> progress)
    {
        Gw2Sharp.WebApi.V2.IApiV2ObjectList<Map> maps = await this.APIManager.Gw2ApiClient.V2.Maps.AllAsync();
        this._maps = maps.ToList();

        return true;
    }

    protected override void Unload()
    {
        base.Unload();
        this._selectedMap = null;
        this._maps = null;
        this._dynamicEvent = null;
    }
}
