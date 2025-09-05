// CharacterView.cs (最终版 - 兼顾绑定与演出)
using UnityEngine;
using System.ComponentModel;
using System.Collections;

// 它实现了我们定义的“供应商”接口IViewModelProvider
public class CharacterView : MonoBehaviour, IViewModelProvider
{
    private CharacterViewModel _viewModel;
    public INotifyPropertyChanged ViewModel => _viewModel; // 实现接口

    // --- 在Inspector中拖拽对特效和动画组件的引用 ---
    [Header("演出组件")]
    public ParticleSystem levelUpEffect;
    public Animator levelTextAnimator;

    private int _previousLevel = -1;

    void Awake()
    {
        // 1. 创建并持有ViewModel，这是它的基础职责
        _viewModel = new CharacterViewModel(GameRoot.CharacterModel);

        // 2. 订阅ViewModel的变化，准备触发“演出”，这是它的核心职责
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        // 初始化上一等级的记录
        _previousLevel = _viewModel.Level;
    }

    // 当ViewModel的属性发生变化时，此方法被调用
    private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        // View只关心那些能触发“演出”的特定属性变化
        if (e.PropertyName == nameof(_viewModel.Level))
        {
            Debug.Log(e.PropertyName);
            // 检查等级是否真的发生了变化，并且是增加了
            if (_viewModel.Level > _previousLevel)
            {
                // 如果等级提升了，就播放升级演出！
                StartCoroutine(PlayLevelUpAnimationSequence());
            }
            _previousLevel = _viewModel.Level;
        }
    }

    // --- 纯粹的UI演出逻辑，与业务数据完全无关 ---
    private IEnumerator PlayLevelUpAnimationSequence()
    {
        Debug.Log("View: 开始播放升级演出！");

        if (levelUpEffect != null) levelUpEffect.Play();
        if (levelTextAnimator != null) levelTextAnimator.SetTrigger("LevelUp");
        // SoundManager.Play("LevelUpSound");

        yield return new WaitForSeconds(1.5f);

        Debug.Log("View: 升级演出播放完毕！");

        // 注意：UI上数字的最终更新，依然是由TextBinder自动完成的。
        // View的演出逻辑，与Binder的数据绑定逻辑，是并行不悖、各司其职的。
    }

    void OnDestroy()
    {
        _viewModel?.Unregister();
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
    }
}