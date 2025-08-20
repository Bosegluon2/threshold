using Godot;
using Godot.Collections;
using System;
using System.Linq;
using Threshold.Core;
using Threshold.Core.Agent;
using Threshold.Core.Data;

namespace Threshold.UI.MissionAssignment
{
    /// <summary>
    /// 任务分配UI - 纯粹的Agent分配界面
    /// </summary>
    public partial class MissionAssignmentUI : Control
    {
        // UI节点引用
        private LineEdit MissionNameInput;
        private OptionButton MissionTypeOption;
        private OptionButton BaseLocationOption;
        private OptionButton TargetLocationOption;
        private HSlider FoodSupplySlider;
        private Label FoodSupplyValue;
        private HSlider WaterSupplySlider;
        private Label WaterSupplyValue;
        private ItemList SelectedAgentsList;
        private Button RemoveAgentButton;
        private Button SubmitMissionButton;
        private Button ClearButton;
        private Button MapButton;
        private Godot.Collections.Dictionary<string, Threshold.Core.Data.Place> availablePlaces = new Godot.Collections.Dictionary<string, Threshold.Core.Data.Place>();
        // 公共数据 - 可以被外部访问和修改
        public Array<Agent> MissionAgents { get; private set; } = new Array<Agent>();
        
        // 私有数据
        private Array<Agent> selectedAgents = new Array<Agent>();
        private int selectedFoodSupply = 0;
        private int selectedWaterSupply = 0;

        // 任务配置数据
        private string missionName = "";
        private string missionType = "";
        private string baseLocation = "";
        private string targetLocation = "";
        [Signal]
        public delegate void MissionAssignedEventHandler();
        public override void _Ready()
        {
            Fader.Instance.FadeIn(1.0f);
            InitializeUI();
            ConnectSignals();
            InitializeData();
        }

        /// <summary>
        /// 初始化UI节点引用
        /// </summary>
        private void InitializeUI()
        {
            // 任务设置区域
            MissionNameInput = GetNode<LineEdit>("ScrollContainer/VBoxContainer/MissionSetupSection/MissionNameContainer/MissionNameInput");
            MissionTypeOption = GetNode<OptionButton>("ScrollContainer/VBoxContainer/MissionSetupSection/MissionTypeContainer/MissionTypeOption");

            // 地点选择区域
            BaseLocationOption = GetNode<OptionButton>("ScrollContainer/VBoxContainer/LocationSection/BaseLocationContainer/BaseLocationOption");
            TargetLocationOption = GetNode<OptionButton>("ScrollContainer/VBoxContainer/LocationSection/TargetLocationContainer/TargetLocationOption");

            // 资源补给区域
            FoodSupplySlider = GetNode<HSlider>("ScrollContainer/VBoxContainer/ResourceSection/FoodSupplyContainer/FoodSupplySlider");
            FoodSupplyValue = GetNode<Label>("ScrollContainer/VBoxContainer/ResourceSection/FoodSupplyContainer/FoodSupplyValue");
            WaterSupplySlider = GetNode<HSlider>("ScrollContainer/VBoxContainer/ResourceSection/WaterSupplyContainer/WaterSupplySlider");
            WaterSupplyValue = GetNode<Label>("ScrollContainer/VBoxContainer/ResourceSection/WaterSupplyContainer/WaterSupplyValue");

            // 人员分配区域
            SelectedAgentsList = GetNode<ItemList>("ScrollContainer/VBoxContainer/AgentSection/SelectedAgentsList");
            RemoveAgentButton = GetNode<Button>("ScrollContainer/VBoxContainer/AgentSection/AgentButtonsContainer/RemoveAgentButton");

            // 操作按钮
            SubmitMissionButton = GetNode<Button>("ScrollContainer/VBoxContainer/ActionSection/SubmitMissionButton");
            ClearButton = GetNode<Button>("ScrollContainer/VBoxContainer/ActionSection/ClearButton");
            MapButton = GetNode<Button>("ScrollContainer/VBoxContainer/Map");
            MapButton.Pressed += () =>
            {
                Fader.Instance.FadeOut(1.0f, Callable.From(() =>
                {
                    Global.Instance.backScene = FastLoader.Instance.files["AssignMission"];
                    GetTree().ChangeSceneToPacked(FastLoader.Instance.files["Map"]);
                }));
            };
        }

        /// <summary>
        /// 连接信号
        /// </summary>
        private void ConnectSignals()
        {
            FoodSupplySlider.ValueChanged += OnFoodSupplyChanged;
            WaterSupplySlider.ValueChanged += OnWaterSupplyChanged;
            RemoveAgentButton.Pressed += OnRemoveAgentPressed;
            SubmitMissionButton.Pressed += OnSubmitMissionPressed;
            ClearButton.Pressed += OnClearPressed;
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitializeData()
        {
            // 初始化任务类型选项
            MissionTypeOption.Clear();
            MissionTypeOption.AddItem("生产任务", (int)MissionType.Production);
            MissionTypeOption.AddItem("探索任务", (int)MissionType.Exploration);
            MissionTypeOption.AddItem("战斗任务", (int)MissionType.Combat);
            MissionTypeOption.AddItem("救援任务", (int)MissionType.Rescue);
            MissionTypeOption.AddItem("运输任务", (int)MissionType.Delivery);
            MissionTypeOption.Selected = 0;

            // 初始化地点选项
            InitializeLocationOptions();

            // 初始化资源滑块
            InitializeResourceSliders();
            RefreshAvailablePlaces();
        }
        private void RefreshAvailablePlaces()
        {
            if (GameManager.Instance?.Library == null) return;

            availablePlaces.Clear();
            BaseLocationOption.Clear();
            TargetLocationOption.Clear();

            // 获取所有地点
            var places = GameManager.Instance.Library.GetAllPlaces();
            foreach (var place in places)
            {
                if (place == null) continue;

                availablePlaces[place.Id] = place;
                var displayName = $"{place.Name} ({place.Type})";
                
                BaseLocationOption.AddItem(displayName);
                TargetLocationOption.AddItem(displayName);
            }

            // 设置默认基地地点（普罗米修斯观测站大厅）
            var basePlace = GameManager.Instance.Library.GetPlace("prometheus_observatory_hall");
            if (basePlace != null)
            {
                for (int i = 0; i < BaseLocationOption.ItemCount; i++)
                {
                    if (BaseLocationOption.GetItemText(i).Contains(basePlace.Name))
                    {
                        BaseLocationOption.Selected = i;
                        break;
                    }
                }
            }
        }
        /// <summary>
        /// 初始化地点选项
        /// </summary>
        private void InitializeLocationOptions()
        {
            BaseLocationOption.Clear();
            TargetLocationOption.Clear();

            // 这里可以从GameManager获取可用地点
            var locations = GameManager.Instance.Library.GetAllPlaces();

            foreach (var location in locations)
            {
                BaseLocationOption.AddItem(location.Name);
                TargetLocationOption.AddItem(location.Name);
            }

            BaseLocationOption.Selected = 0;
            TargetLocationOption.Selected = 1;
        }

        /// <summary>
        /// 初始化资源滑块
        /// </summary>
        private void InitializeResourceSliders()
        {
            FoodSupplySlider.MinValue = 0;
            FoodSupplySlider.MaxValue = 100;
            FoodSupplySlider.Value = 0;
            FoodSupplySlider.Step = 1;

            WaterSupplySlider.MinValue = 0;
            WaterSupplySlider.MaxValue = 100;
            WaterSupplySlider.Value = 0;
            WaterSupplySlider.Step = 1;

            selectedFoodSupply = 0;
            selectedWaterSupply = 0;
            FoodSupplyValue.Text = "0";
            WaterSupplyValue.Text = "0";
        }

        /// <summary>
        /// 更新已选人员列表
        /// </summary>
        private void UpdateSelectedAgentsList()
        {
            SelectedAgentsList.Clear();
            foreach (var agent in selectedAgents)
            {
                if (agent == null) continue;
                var displayText = $"{agent.AgentName} (健康: {agent.CurrentHealth}, 能量: {agent.CurrentEnergy})";
                SelectedAgentsList.AddItem(displayText);
            }
        }

        /// <summary>
        /// 食物补给变化处理
        /// </summary>
        private void OnFoodSupplyChanged(double value)
        {
            selectedFoodSupply = (int)value;
            FoodSupplyValue.Text = selectedFoodSupply.ToString();
        }

        /// <summary>
        /// 水源补给变化处理
        /// </summary>
        private void OnWaterSupplyChanged(double value)
        {
            selectedWaterSupply = (int)value;
            WaterSupplyValue.Text = selectedWaterSupply.ToString();
        }


        /// <summary>
        /// 移除人员按钮处理
        /// </summary>
        private void OnRemoveAgentPressed()
        {
            var selectedIndex = SelectedAgentsList.GetSelectedItems();
            if (selectedIndex.Length == 0) return;

            var selectedIndexValue = selectedIndex[0];
            if (selectedIndexValue < 0 || selectedIndexValue >= selectedAgents.Count) return;

            var agent = selectedAgents[selectedIndexValue];
            if (agent != null)
            {
                selectedAgents.RemoveAt(selectedIndexValue);
                UpdateSelectedAgentsList();
            }
        }

        /// <summary>
        /// 提交任务按钮处理
        /// </summary>
        private void OnSubmitMissionPressed()
        {
            // 基础验证
            if (string.IsNullOrEmpty(MissionNameInput.Text))
            {
                UpdateMissionStatus("[color=red]错误：请输入任务名称[/color]");
                return;
            }

            if (selectedAgents.Count == 0)
            {
                UpdateMissionStatus("[color=red]错误：请选择至少一个人员[/color]");
                return;
            }

            if (BaseLocationOption.Selected < 0 || TargetLocationOption.Selected < 0)
            {
                UpdateMissionStatus("[color=red]错误：请选择基地地点和目标地点[/color]");
                return;
            }

            // 检查出发地和目的地是否相同
            if (BaseLocationOption.Selected == TargetLocationOption.Selected)
            {
                UpdateMissionStatus("[color=red]错误：出发地点和目标地点不能相同[/color]");
                return;
            }

            try
            {
                // 检查任务名称是否重复
                if (GameManager.Instance?.MissionManager?.HasMissionWithName(MissionNameInput.Text) == true)
                {
                    UpdateMissionStatus("[color=red]错误：任务名称已存在，请使用不同的名称[/color]");
                    return;
                }

                // 获取选择的地点
                var basePlace = GetSelectedPlace(BaseLocationOption.Selected);
                var targetPlace = GetSelectedPlace(TargetLocationOption.Selected);
                
                if (basePlace == null || targetPlace == null)
                {
                    UpdateMissionStatus("[color=red]错误：无法获取选择的地点[/color]");
                    return;
                }

                // 检查任务可行性
                var missionType = (MissionType)MissionTypeOption.GetSelectedId();
                if (!IsMissionFeasible(missionType, targetPlace))
                {
                    UpdateMissionStatus($"[color=red]错误：目标地点 {targetPlace.Name} 不适合执行 {GetMissionTypeName(missionType)} 任务[/color]");
                    return;
                }

                // 检查资源是否充足
                if (!CheckResourceAvailability(selectedFoodSupply, selectedWaterSupply))
                {
                    UpdateMissionStatus($"[color=red]错误：仓库资源不足，无法提供所需的补给[/color]");
                    return;
                }

                // 更新任务配置数据
                missionName = MissionNameInput.Text;
                missionType = GetMissionType(GetMissionTypeName((MissionType)MissionTypeOption.GetSelectedId()));
                baseLocation = basePlace.Name;
                targetLocation = targetPlace.Name;

                // 更新公共的MissionAgents数组
                UpdateMissionAgents();
                GD.Print("任务信息：");
                GD.Print("任务名称：" + missionName);
                GD.Print("任务类型：" + missionType);
                GD.Print("出发地点：" + baseLocation);
                GD.Print("目标地点：" + targetLocation);
                GD.Print("食物补给：" + selectedFoodSupply);
                GD.Print("水源补给：" + selectedWaterSupply);
                GD.Print("参与人员：" + selectedAgents.Count);
                // 通过MissionManager创建任务
                var mission = GameManager.Instance.MissionManager.CreateMission(
                    MissionNameInput.Text,
                    missionType,
                    1.0f, // 基础难度设为1.0，实际难度由MissionSimulator动态计算
                    basePlace,
                    targetPlace,
                    selectedAgents,
                    selectedFoodSupply,
                    selectedWaterSupply
                );

                if (mission == null)
                {
                    UpdateMissionStatus("[color=red]错误：创建任务失败[/color]");
                    return;
                }

                // 从仓库扣除资源
                if (!ConsumeMissionResources(selectedFoodSupply, selectedWaterSupply))
                {
                    UpdateMissionStatus("[color=red]错误：扣除资源失败[/color]");
                    return;
                }

                // 开始任务
                if (GameManager.Instance.MissionManager.StartMission(mission))
                {
                    UpdateMissionStatus($"[color=green]任务 {mission.MissionName} 已开始！[/color]");
                    EmitSignal(SignalName.MissionAssigned);
                    // 提交成功后清除UI数据
                    ClearUIData();
                }
                else
                {
                    UpdateMissionStatus("[color=red]错误：开始任务失败[/color]");
                }
            }
            catch (Exception ex)
            {
                UpdateMissionStatus($"[color=red]错误：{ex.Message}[/color]");
            }
        }

        private string GetMissionTypeName(MissionType missionType)
        {
            return missionType switch
            {
                MissionType.Production => "生产",
                MissionType.Exploration => "探索",
                MissionType.Combat => "战斗",
                MissionType.Rescue => "救援",
                MissionType.Delivery => "运输",
                MissionType.Investigation => "调查",
                _ => "未知"
            };
        }
        private MissionType GetMissionType(string missionTypeName)
        {
            return missionTypeName switch
            {
                "生产" => MissionType.Production,
                "探索" => MissionType.Exploration,
                "战斗" => MissionType.Combat,
                "救援" => MissionType.Rescue,
                "运输" => MissionType.Delivery,
                "调查" => MissionType.Investigation,
                _ => MissionType.Production
            };
        }


        private bool CheckResourceAvailability(int foodAmount, int waterAmount)
        {
            if (GameManager.Instance?.ResourceManager == null) return false;

            var foodResource = GameManager.Instance.ResourceManager.GetResource("food");
            var waterResource = GameManager.Instance.ResourceManager.GetResource("water");

            if (foodResource == null || waterResource == null) return false;

            return foodResource.IsSufficient(foodAmount) && waterResource.IsSufficient(waterAmount);
        }


        private bool IsMissionFeasible(MissionType missionType, Threshold.Core.Data.Place targetPlace)
        {
            if (targetPlace == null) return false;

            return missionType switch
            {
                MissionType.Production => targetPlace.Type == "production",
                MissionType.Exploration => targetPlace.Type == "exploration" || targetPlace.Type == "landmark",
                MissionType.Combat => targetPlace.Type == "combat" || targetPlace.Type == "dangerous",
                MissionType.Rescue => targetPlace.Type == "rescue" || targetPlace.Type == "medical",
                MissionType.Delivery => targetPlace.Type == "delivery" || targetPlace.Type == "transport",
                MissionType.Investigation => targetPlace.Type == "investigation" || targetPlace.Type == "research",
                _ => true // 其他类型默认允许
            };
        }


        private bool ConsumeMissionResources(int selectedFoodSupply, int selectedWaterSupply)
        {
            if (selectedFoodSupply > GameManager.Instance.ResourceManager.GetResource("food").CurrentAmount)
            {
                return false;
            }
            if (selectedWaterSupply > GameManager.Instance.ResourceManager.GetResource("water").CurrentAmount)
            {
                return false;
            }
            GameManager.Instance.ResourceManager.ConsumeResource("food", selectedFoodSupply, "任务消耗");
            GameManager.Instance.ResourceManager.ConsumeResource("water", selectedWaterSupply, "任务消耗");
            return true;
        }


        private Threshold.Core.Data.Place GetSelectedPlace(int selectedIndex)
        {
            if (selectedIndex < 0 || selectedIndex >= availablePlaces.Count) return null;

            var placeList = new Array<Threshold.Core.Data.Place>(availablePlaces.Values);
            return placeList[selectedIndex];
        }

        private void UpdateMissionStatus(string message)
        {
            GetNode<RichTextLabel>("ScrollContainer/VBoxContainer/Message").Text = message;
        }

        /// <summary>
        /// 清除按钮处理
        /// </summary>
        private void OnClearPressed()
        {
            ClearUIData();
        }

        /// <summary>
        /// 更新公共的MissionAgents数组
        /// </summary>
        private void UpdateMissionAgents()
        {
            MissionAgents.Clear();
            foreach (var agent in selectedAgents)
            {
                if (agent != null)
                {
                    MissionAgents.Add(agent);
                }
            }
        }

        /// <summary>
        /// 清除UI数据
        /// </summary>
        private void ClearUIData()
        {
            // 清除输入
            MissionNameInput.Text = "";
            MissionTypeOption.Selected = 0;
            BaseLocationOption.Selected = 0;
            TargetLocationOption.Selected = 1;

            // 清除资源选择
            FoodSupplySlider.Value = 0;
            WaterSupplySlider.Value = 0;
            selectedFoodSupply = 0;
            selectedWaterSupply = 0;
            FoodSupplyValue.Text = "0";
            WaterSupplyValue.Text = "0";

            // 清除人员选择
            selectedAgents.Clear();
            UpdateSelectedAgentsList();

            // 清除任务配置数据
            missionName = "";
            missionType = "";
            baseLocation = "";
            targetLocation = "";

            // 清除公共数组
            MissionAgents.Clear();
        }

        // ==================== 公共访问函数 ====================

        /// <summary>
        /// 获取任务名称
        /// </summary>
        public string GetMissionName() => missionName;

        /// <summary>
        /// 获取任务类型
        /// </summary>
        public string GetMissionType() => missionType;

        /// <summary>
        /// 获取出发地点
        /// </summary>
        public string GetBaseLocation() => baseLocation;

        /// <summary>
        /// 获取目标地点
        /// </summary>
        public string GetTargetLocation() => targetLocation;

        /// <summary>
        /// 获取食物补给数量
        /// </summary>
        public int GetFoodSupply() => selectedFoodSupply;

        /// <summary>
        /// 获取水源补给数量
        /// </summary>
        public int GetWaterSupply() => selectedWaterSupply;

        /// <summary>
        /// 检查是否已分配人员
        /// </summary>
        public bool HasAssignedAgents() => MissionAgents.Count > 0;

        /// <summary>
        /// 获取已分配人员数量
        /// </summary>
        public int GetAssignedAgentCount() => MissionAgents.Count;

        /// <summary>
        /// 清除所有数据并重置到默认状态
        /// </summary>
        public void ClearAllData()
        {
            MissionAgents.Clear();
            selectedAgents.Clear();
            ClearUIData();
        }


        /// <summary>
        /// 添加单个可用人员（外部调用）
        /// </summary>
        public void AddAgent(Agent agent)
        {
            if (agent != null && !selectedAgents.Contains(agent))
            {
                selectedAgents.Add(agent);
                UpdateSelectedAgentsList();
            }
        }
        private void UpdateResourceSliderRanges()
        {
            if (FoodSupplySlider == null || WaterSupplySlider == null) return;

            // 创建临时任务来计算资源上限
            var tempMission = new MissionSimulator();
            foreach (var agent in selectedAgents)
            {
                if (agent != null)
                {
                    tempMission.AddAgent(agent);
                }
            }

            var resourceLimits = tempMission.GetResourceLimits();

            // 设置食物滑块范围
            FoodSupplySlider.MinValue = 0;
            FoodSupplySlider.MaxValue = resourceLimits.MaxFood;
            FoodSupplySlider.Value = Math.Min(selectedFoodSupply, resourceLimits.MaxFood);

            // 设置水源滑块范围
            WaterSupplySlider.MinValue = 0;
            WaterSupplySlider.MaxValue = resourceLimits.MaxWater;
            WaterSupplySlider.Value = Math.Min(selectedWaterSupply, resourceLimits.MaxWater);

            // 更新显示值
            selectedFoodSupply = (int)FoodSupplySlider.Value;
            selectedWaterSupply = (int)WaterSupplySlider.Value;
            FoodSupplyValue.Text = selectedFoodSupply.ToString();
            WaterSupplyValue.Text = selectedWaterSupply.ToString();

            // 清理临时任务
            tempMission.QueueFree();
        }
        /// <summary>
        /// 移除单个可用人员（外部调用）
        /// </summary>
        public void RemoveAgent(Agent agent)
        {
            if (agent != null && selectedAgents.Contains(agent))
            {
                selectedAgents.Remove(agent);
                UpdateSelectedAgentsList();
            }
        }


    }
}
