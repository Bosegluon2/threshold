using Godot;
using Godot.Collections;
using System;

public partial class GameData : Node
{

	public static GameData Instance { get; private set; }
	public readonly Dictionary<string, string> Reference = new()
	{
		{"warfare","[center][b]Warfare（战斗技能）[/b][/center]\n此属性包含近身格斗与武器精通。决定角色战斗能力，对冷战科技下的武器运用熟练度至关重要。"},
		{"adaptability","[center][b]Adaptability（适应能力）[/b][/center]\n涵盖变异抵抗和生存本能。体现角色在变异生物环境中的适应生存能力。"},
		{"reasoning","[center][b]Reasoning（推理能力）[/b][/center]\n包括战术规划和科技理解。决定角色思考、规划及对冷战科技物品的运用能力。"},
		{"perception","[center][b]Perception（感知能力）[/b][/center]\n由危险感知和观察力组成。帮助角色察觉危险和发现环境细节。"},
		{"endurance","[center][b]Endurance（耐力）[/b][/center]\n包含体力和健康值。决定角色行动持久性和承受伤害的能力。"},
		{"dexterity","[center][b]Dexterity（敏捷性）[/b][/center]\n由反射神经和潜行组成。影响角色反应速度和潜行能力。"}
	};
		public readonly  Dictionary<string,string> KeyMap = new Dictionary<string,string>
	{
		{"W","warfare"},
		{"A","adaptability"},
		{"R","reasoning"},
		{"P","perception"},
		{"E","endurance"},
		{"D","dexterity"}

	};
	public readonly Dictionary<string, Dictionary<string, Variant>> Specialty = new Dictionary<string, Dictionary<string, Variant>>{
		{"red_mist_resilience",
			new Dictionary<string,Variant>{
				{ "name","红雾免疫" },
				{ "description","免疫红雾伤害，在红雾中视野范围提升20%" },
				{"requirement",new Dictionary<string,Variant>{{"adaptability",15}}}
			}
		},
		{"heavy_breaker",
			new Dictionary<string,Variant>{
				{ "name","重型破坏" },
				{ "description","可破坏加固木门和铁栅栏，破坏速度随Warfare值提升" },
				{"requirement",new Dictionary<string,Variant>{{"warfare",8}, {"endurance",10}}}
			}
		},
		{"quick_decryption",
			new Dictionary<string,Variant>{
				{ "name","快速破译" },
				{ "description","密码类谜题破解时间减少40%，错误尝试不消耗次数" },
				{"requirement",new Dictionary<string,Variant>{{"reasoning",8}, {"perception",7}}}
			}
		},
		{"silent_movement",
			new Dictionary<string,Variant>{
				{ "name","无声潜行" },
				{ "description","移动噪音降低70%，怪物察觉范围缩小至原30%" },
				{"requirement",new Dictionary<string,Variant>{{"dexterity",7}, {"adaptability",5}}}
			}
		},
		{"pain_suppression",
			new Dictionary<string,Variant>{
				{ "name","疼痛抑制" },
				{ "description","生命值低于20%时，伤害减免提升50%，不会因受伤减速" },
				{"requirement",new Dictionary<string,Variant>{{"endurance",5}, {"warfare",7}}}
			}
		},
		{"environmental_insight",
			new Dictionary<string,Variant>{
				{ "name","环境洞察" },
				{ "description","自动标记场景中隐藏线索和可互动物品，持续显示5秒" },
				{"requirement",new Dictionary<string,Variant>{{"perception",7}, {"reasoning",11}}}
			}
		},
		{"emergency_repair",
			new Dictionary<string,Variant>{
				{ "name","紧急维修" },
				{ "description","可修复损坏的电子设备（如门禁、发电机），成功率基于相关属性" },
				{"requirement",new Dictionary<string,Variant>{{"reasoning",6}, {"dexterity",10}}}
			}
		}
	};
	public Dictionary Pack;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
