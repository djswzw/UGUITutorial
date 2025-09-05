public class CharacterViewModel : BaseViewModel
{
    private readonly CharacterModel model;

    // --- 为View准备的、可直接绑定的属性 ---
    // 注意属性名要与Binder中填写的字符串一致
    public string LevelText => "Lv: " + model.Level;
    public float ExpRatio => (float)model.CurrentExp / model.ExpToNextLevel;
    public string AttackText => "攻击力: " + model.Attack;
    public string DefenseText => "防御力: " + model.Defense;
    public string NameText => model.Name;

    public int Level => model.Level;

    public ICommand LevelUpCommand { get; }

    public CharacterViewModel(CharacterModel characterModel)
    {
        this.model = characterModel;
        LevelUpCommand = new RelayCommand(ExecuteLevelUp, CanExecuteLevelUp);
        // 订阅Model的变化
        this.model.OnDataChanged += OnModelDataChanged;
    }

    private void OnModelDataChanged()
    {
        // 当底层Model变化时，通知所有绑定了这个ViewModel的UI进行更新
        OnPropertyChanged(nameof(LevelText));
        OnPropertyChanged(nameof(ExpRatio));
        OnPropertyChanged(nameof(AttackText));
        OnPropertyChanged(nameof(DefenseText));
        OnPropertyChanged(nameof(Level));
        // 手动通知命令更新其可用状态
        (LevelUpCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private void ExecuteLevelUp() => model.TryLevelUp();
    private bool CanExecuteLevelUp() => model.CurrentExp >= model.ExpToNextLevel;

    public void Unregister() => this.model.OnDataChanged -= OnModelDataChanged;
}