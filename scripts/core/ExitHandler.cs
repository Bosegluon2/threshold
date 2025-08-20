using Godot;

public partial class ExitHandler : Node
{
    UiLayer Exit;
    public static ExitHandler Instance;
    public override void _EnterTree()
    {
        base._EnterTree();
        Instance = this;
        
    }
    // 节点进入场景树时调用
    public override void _Ready()
    {
    }

    // 处理系统通知
    public override void _Notification(int what)
    {
        base._Notification(what);
        
        // 检测窗口关闭请求
        if (what == NotificationWMCloseRequest)
        {

            RequestExit();
            
        }
    }
    public void RequestExit()
    {
            Exit = GetTree().Root.GetNodeOrNull<UiLayer>("UiLayer");
            if (Exit != null)
            {
                GD.Print("存在UiLayer节点");
                if (!Exit.Visible)
                {
                    Exit.ShowUp();
                }
               
            }
            else
            {
                GD.Print("不存在UiLayer节点");
                Exit = FastLoader.Instance.files["UiLayer"].Instantiate<UiLayer>();
                Exit.Name = "UiLayer";
                Exit.Message = "Are you sure \nyou want to exit?";
                Exit.leftButtonMessage = "No";
                Exit.rightButtonMessage = "Yes";
                GetTree().Root.AddChild(Exit);
                Exit.ShowUp();
            }
            Exit.ButtonPressed += OnConfirm;
            GetTree().AutoAcceptQuit = false;
            // 在这里添加退出前需要执行的代码
            GD.Print("检测到窗口关闭请求，执行退出前清理...");
    }
    private void OnConfirm(string type)
    {
        if (type == "left")
        {
            Exit.HideDown();
        }
        else if (type == "right")
        {
            SaveGameData();
        }
    }
    // 自定义方法：保存游戏数据
    private void SaveGameData()
    {
        // 实现游戏数据保存逻辑
        GD.Print("游戏数据已保存");
        GetTree().Quit();
    }
}