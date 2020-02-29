using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainThread : MonoBehaviour
{
    public static MainThread mainThread;
    public GameObject Drop;
    public GameObject StartButtonText;
    public int totalDropsAmount = 2;
    public bool GameStarted = false; // To prevent from vanishing when generating drops
    public int score = 0;
    public GameObject scoreText;
    public GameObject targetScoreText;
    public int dropsCount;

    // for multiple levels
    private float[] initialDropsRadiusList = new float[11];
    public int currentLevel = 0;
    public int targetScore = 0;

    private void Awake() {
        if(mainThread == null){
            mainThread = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {   
        // Screen.SetResolution(1920, 1080, true, 60);
        dropsCount = 0;
        scoreText.GetComponent<Text>().text = score.ToString();
        initTarget();
        targetScoreText.GetComponent<Text>().text = targetScore.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        scoreText.GetComponent<Text>().text = score.ToString();
        if (dropsCount<=1){
            GameStarted = false;
            StartButtonText.GetComponent<Text>().text = "Start";
            if(score<targetScore){
                scoreText.GetComponent<Text>().text = "Game Over!";
            }else{
                currentLevel ++;
                initTarget();
                targetScoreText.GetComponent<Text>().text = targetScore.ToString();
                SceneManager.LoadScene("LevelScene");
            }
        }
    }

    // Initialization
    public void initTarget(){
        for (; dropsCount < totalDropsAmount; dropsCount++) {
            Drop.GetComponent<DropsProperty>().id = dropsCount;
            Drop.GetComponent<DropsProperty>().r = UnityEngine.Random.Range(50.0f, 150.0f);
            initialDropsRadiusList[dropsCount] = System.Convert.ToInt16(Drop.GetComponent<DropsProperty>().r);
            Drop.transform.localScale = new Vector2(Drop.GetComponent<DropsProperty>().r, Drop.GetComponent<DropsProperty>().r);
            // keep score label original size
            // Drop.transform.GetChild(0).gameObject.transform.localScale = new Vector2(1/Drop.GetComponent<DropsProperty>().r, 1/Drop.GetComponent<DropsProperty>().r);
            Instantiate(Drop, new Vector3(UnityEngine.Random.Range(200.0f, 1600.0f), UnityEngine.Random.Range(100.0f, 800.0f), 0.0f), Quaternion.identity);
        }
        targetScore = getTargetScore(currentLevel);
    }
    // start/pause game
    public void gamestart() {
        GameStarted = !GameStarted;
        if (GameStarted){
            StartButtonText.GetComponent<Text>().text = "Pause";
        }else{
            StartButtonText.GetComponent<Text>().text = "Start";
        }
    }

    // Calculate target score for different levels
    private int getTargetScore(int level){
        float difFactorDeno = 5; // larger -> easier
        float difFactorBase = 2; // larger -> harder
        float difficulty = 1 - Mathf.Pow(difFactorBase, ((float)level)*(-1)/difFactorDeno); // compute difficulty factor, larger -> easier
        float maxPossibleScore = 0;
        for (int i = 0; i < initialDropsRadiusList.Length; i++)
        {
            Array.Sort(initialDropsRadiusList);
            if (initialDropsRadiusList[0]<int.MaxValue){
                maxPossibleScore += initialDropsRadiusList[0]/4;
                initialDropsRadiusList[1] += initialDropsRadiusList[0]/2;
                initialDropsRadiusList[0] = int.MaxValue;
            }else break;
        }
        return System.Convert.ToUInt16(maxPossibleScore*difficulty)+score;
    }
}