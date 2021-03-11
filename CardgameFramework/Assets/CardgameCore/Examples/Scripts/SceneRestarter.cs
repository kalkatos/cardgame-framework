﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneRestarter : MonoBehaviour
{
	public void RestartScene ()
	{
		SceneManager.LoadScene(SceneManager.GetActiveScene().name);
	}
}
