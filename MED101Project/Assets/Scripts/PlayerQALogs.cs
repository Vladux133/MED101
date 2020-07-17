using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;


public class PlayerQALogs : MonoBehaviour

{
    [Range(0,60)]
    public int LogsPerSecond; // As game aims to run at 60fps, means how many logs there should be during these 60 frames;
    private GameObject player;
    private int _currentCount;
    private int _maxCount;
    private string _playerX;
    private string _playerY;
    private string _playerZ;
    private StreamWriter _streamWriter;
    private string _currentFileName;



    // Start is called before the first frame update

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        _currentCount = 0;
        _maxCount = 60 / LogsPerSecond;
        _currentFileName = "Test" + DateTime.Now.Day.ToString()+"-"+DateTime.Now.Hour.ToString()+"-"+DateTime.Now.Minute.ToString() + ".txt" ;
        PcCreateFile();
    }

    // Update is called once per frame

    void Update()
    {
     _currentCount++;

        if (_currentCount == _maxCount)

        {

            int xLenght = player.transform.position.x.ToString().Length;

            int yLenght = player.transform.position.y.ToString().Length;

            int zLenght = player.transform.position.z.ToString().Length;

            if (xLenght < 4)

                _playerX = player.transform.position.x.ToString().Substring(0, xLenght);

            else

                _playerX = player.transform.position.x.ToString().Substring(0, 4);

            if (yLenght < 4)

                _playerY = player.transform.position.y.ToString().Substring(0, xLenght);

            else

                _playerY = player.transform.position.y.ToString().Substring(0, 4);

            if (zLenght < 4)

                _playerZ = player.transform.position.z.ToString().Substring(0, zLenght);

            else

                _playerZ = player.transform.position.z.ToString().Substring(0, 4);

            _currentCount = 0;

            WriteToPC(_playerX + "," + _playerY + "," + _playerZ);

        }

    }


    private void OnApplicationQuit()

    {

        using (StreamWriter sw = File.AppendText(_currentFileName))

            sw.Close();

    }

    private void PcCreateFile()

    {

        if (!Directory.Exists(_currentFileName))

        {

            using (StreamWriter sw = File.CreateText(_currentFileName))

            {

                sw.WriteLine("HeatMapData: X Y Z");

            }

        }

    }


    private void WriteToPC(string message)

    {

        using (StreamWriter sw = File.AppendText(_currentFileName))

        {

            sw.WriteLine(message);

        }

    }

    }
