using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using SimpleJSON;

public class MainThread : MonoBehaviour
{
    public static MainThread mainThread;
    public GameObject Drop;
    public GameObject StartButton;
    public GameObject ScoreRankPanel;
    public int totalDropsAmount = 4;
    public bool GameStarted = false; // To prevent from vanishing when generating drops
    public int score = 0;
    public GameObject scoreText;
    public GameObject targetScoreText;
    public int dropsCount; // amount of remaining/initialized drops

    // for multiple levels
    private float[] initialDropsRadiusList; // to calculate max possible score
    private float[,] dropsPositions; // to prevent drops from overlaping
    public int currentLevel = 0;
    public int targetScore = 0;

    // For Data Saving
    private const string PLAY_PREF_KEY_HS = "High Score";
    private const string FILE_SCORE_RANK = "/score_rank.txt";
    private bool isGameEnded = false;
    public string playerName = "new player";
    public GameObject inputPlayerName;
    public GameObject playerNameText;
    private const string fetchURL = "http://www.jonysandyk.com:8883/UnityDropGameData";
    private const string updateURL = "http://www.jonysandyk.com:8883/UnityDropGameDataUpdate";

    // for debuging
    bool isDebugLocal = true;


    private void Awake() {
        if(mainThread == null){
            initialDropsRadiusList = new float[totalDropsAmount+1];
            dropsPositions = new float[totalDropsAmount+1,2];
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
        gameReset(0);
        StartButton.GetComponent<Button>().onClick.AddListener(StartButtonCliked);
    }

    // Update is called once per frame
    void Update()
    {
        scoreText.GetComponent<Text>().text = "Score: "+score.ToString();
        if (GameObject.FindGameObjectsWithTag("Drop").Length <= 1 && GameStarted){
            if(score<targetScore){
                gameEnded();
            }else{
                currentLevel ++;
                initTarget();
                targetScoreText.GetComponent<Text>().text = "Target Score: "+targetScore.ToString();
                // SceneManager.LoadScene("LevelScene");
            }
        }
    }

    // Initialization
    public void initTarget(){
        if(dropsCount>0){
            GameObject firstDrop = GameObject.FindGameObjectsWithTag("Drop")[0];
            dropsPositions[0, 0] = firstDrop.transform.position.x;
            dropsPositions[0, 1] = firstDrop.transform.position.y;
            initialDropsRadiusList[0] = firstDrop.GetComponent<DropsProperty>().r;
        }
        float x;
        float y;
        while (dropsCount < totalDropsAmount) {
            Drop.GetComponent<DropsProperty>().id = dropsCount;
            x = UnityEngine.Random.Range(200.0f, 1600.0f);
            y = UnityEngine.Random.Range(100.0f, 800.0f);
            Drop.GetComponent<DropsProperty>().r = UnityEngine.Random.Range(50.0f, 150.0f);
            Drop.transform.localScale = new Vector2(Drop.GetComponent<DropsProperty>().r, Drop.GetComponent<DropsProperty>().r);
            // check if overlap
            bool isOverlap = false;
            for (int i = 0; i < dropsCount; i++){
                if (Mathf.Pow(dropsPositions[i, 0]-x, 2)+Mathf.Pow(dropsPositions[i, 1]-y, 2) < Mathf.Pow(Drop.GetComponent<DropsProperty>().r + initialDropsRadiusList[i], 2)){
                    isOverlap = true;
                    break;
                }
            }
            if(!isOverlap){
                dropsPositions[dropsCount, 0] = x;
                dropsPositions[dropsCount, 1] = y;
                initialDropsRadiusList[dropsCount] = Drop.GetComponent<DropsProperty>().r;
                Instantiate(Drop, new Vector3(x, y, 0.0f), Quaternion.identity);
                dropsCount++;
            }
        }
        targetScore = getTargetScore(currentLevel);
    }

    // Calculate target score for different levels
    private int getTargetScore(int level){
        float difFactorDeno = 10; // larger -> easier
        float difFactorBase = 2; // larger -> harder
        float difficulty = 1 - Mathf.Pow(difFactorBase, ((float)level)*(-1)/difFactorDeno); // compute difficulty factor, larger -> easier
        float maxPossibleScore = 0;
        for (int i = 0; i < initialDropsRadiusList.Length; i++)
        {
            // find the smallest one in the remaining drops
            Array.Sort(initialDropsRadiusList);
            if (initialDropsRadiusList[0]<float.MaxValue){
                maxPossibleScore += initialDropsRadiusList[0]/4;
                initialDropsRadiusList[1] += initialDropsRadiusList[0]/2;
                initialDropsRadiusList[0] = float.MaxValue;
            }else break;
        }
        return System.Convert.ToUInt16(maxPossibleScore*difficulty)+score;
    }

    // load rank data
    private void loadData(bool isNewScore){
        // fetch rank from server
        List<int> scoreRank = new List<int>();
        List<string> nameRank = new List<string>();
        string scoretext = "";
        bool highScoreInterted = false;
        playerName = playerNameText.GetComponent<Text>().text;
        UnityWebRequest getRequest = new UnityWebRequest(fetchURL, "GET");
        var rankListJson = JSON.Parse("{}");
        if(!isDebugLocal){
            getRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            getRequest.SendWebRequest();
        }
        if(getRequest.isNetworkError || getRequest.isHttpError || isDebugLocal) {
            Debug.Log(getRequest.error);
            // read local file - previous version
            string hsString=""; // high score list as a string
            if (File.Exists(Application.dataPath + FILE_SCORE_RANK)){
                hsString = File.ReadAllText(Application.dataPath + FILE_SCORE_RANK);
            }else {
                hsString = "0,0,0,0,0,0,0,0,0,";
            }
            string[] splitString = hsString.TrimEnd(',').Split(',');
            scoreRank = new List<int>(Array.ConvertAll(splitString, int.Parse));
            scoretext = ""; // to render rank list
            highScoreInterted = false; // check if new score
            string allScoreString = ""; // to write score file
            for (int i = 0; i < scoreRank.Count; i++){
                // update rank list
                if (score>scoreRank[i] && !highScoreInterted && isNewScore){
                    for (int j = scoreRank.Count-1; j > i; j--){
                        scoreRank[j] = scoreRank[j-1];
                    }
                    scoreRank[i] = score;
                    highScoreInterted = true;
                    scoretext += "* ";
                }
                scoretext += ("("+(i+1)+")"+". "+'\t'+scoreRank[i]+'\n');
                allScoreString = allScoreString + scoreRank[i] + ",";
            }
            ScoreRankPanel.transform.GetChild(1).GetComponent<Text>().text = scoretext;
            File.WriteAllText(Application.dataPath + FILE_SCORE_RANK, allScoreString);
        }
        else {
            while(getRequest.downloadProgress < 1){}
            rankListJson = JSON.Parse(getRequest.downloadHandler.text);
            for (int i = 0; i < rankListJson.Count; i++){
                nameRank.Add(rankListJson[i]["name"].Value);
                scoreRank.Add(Int16.Parse(rankListJson[i]["score"].Value));
            }
            for (int i = 0; i < scoreRank.Count; i++){
                // update rank list
                if (score>scoreRank[i] && isNewScore && !highScoreInterted){
                    scoreRank.Insert(i, score);
                    nameRank.Insert(i, playerName);
                    scoreRank.RemoveAt(scoreRank.Count-1);
                    nameRank.RemoveAt(nameRank.Count-1);
                    highScoreInterted = true;
                    scoretext += "* ";
                }
                scoretext += ("("+(i+1)+")"+". "+'\t'+ nameRank[i] + '\t' + scoreRank[i]+'\n');
            }
            ScoreRankPanel.transform.GetChild(1).GetComponent<Text>().text = scoretext;
        }

        // upload new high score to server
        if(isNewScore && !isDebugLocal){
            if (playerName == "") {playerName = "New Player";}
            string json = JsonUtility.ToJson(new playerInfo(playerName, score));
            Debug.Log(json);
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
            UnityWebRequest uwr = new UnityWebRequest(updateURL, "POST");
            uwr.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
            uwr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");
            uwr.SendWebRequest();
            if (uwr.isNetworkError || uwr.isHttpError){
                Debug.Log(uwr.error);
            }else{
                Debug.Log("Form upload complete!");
            }
        }
    }

    // start/pause game
    public void gameStart() {
        GameStarted = !GameStarted;
        if (GameStarted){
            StartButton.transform.GetChild(0).GetComponent<Text>().text = "Pause";
            inputPlayerName.GetComponent<InputField>().interactable = false;
        }else{
            StartButton.transform.GetChild(0).GetComponent<Text>().text = "Start";
            inputPlayerName.GetComponent<InputField>().interactable = true;
        }
    }

    // Game Ends, read and saving data
    private void gameEnded(){
        GameStarted = false;
        isGameEnded = true;
        scoreText.GetComponent<Text>().text = "Game Over!, Your score is: "+score.ToString();
        loadData(isNewScore: true);
        ScoreRankPanel.SetActive(true);
    }

    private void gameReset(int initDropCount){
        isGameEnded = false;
        score = 0;
        currentLevel = 0;
        targetScore = 0;
        ScoreRankPanel.SetActive(false);
        scoreText.GetComponent<Text>().text = "Score: "+score.ToString();
        dropsCount = initDropCount;
        initTarget();
        loadData(isNewScore: false);
        targetScoreText.GetComponent<Text>().text = "Target Score: "+targetScore.ToString();
    }

    private void StartButtonCliked(){
        if(isGameEnded){
            gameReset(1);
        }
        gameStart();
        ScoreRankPanel.SetActive(GameStarted ? false:true);
    }
}