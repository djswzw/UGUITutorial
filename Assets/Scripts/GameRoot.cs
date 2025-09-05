using UnityEngine;

public class GameRoot : MonoBehaviour
{
    public static CharacterModel CharacterModel;
    public GameObject view;
    private void Awake()
    {
        // 初始化游戏根目录
        DontDestroyOnLoad(gameObject);
        CharacterModel = new CharacterModel();
    }

    private void Start()
    {
        view.SetActive(true);
    }

    public void AddExperience(int experience)
    {
        CharacterModel.AddExperience(experience);
    }
}