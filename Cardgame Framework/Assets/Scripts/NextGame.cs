using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NextGame : MonoBehaviour
{
    public void GetNextGame()
	{
		int nextScene = SceneManager.GetActiveScene().buildIndex;
		nextScene++;
		if (nextScene >= SceneManager.sceneCountInBuildSettings)
			nextScene = 1;
		SceneManager.LoadScene(nextScene);
	}
}
