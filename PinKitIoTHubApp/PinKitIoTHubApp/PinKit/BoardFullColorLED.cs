using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace PinKit
{
    public class BoardFullColorLED
    {
               // 各 LED のピン番号
        private const Cpu.Pin pinRed = (Cpu.Pin)0x6d;       // 赤
        private const Cpu.Pin pinGreen = (Cpu.Pin)0x6e;     // 緑
        private const Cpu.Pin pinBlue = (Cpu.Pin)0x6f;      // 青

        // 各 LED が接続されるポート
        private OutputPort portRed;     // 赤
        private OutputPort portGreen;   // 緑
        private OutputPort portBlue;    // 青


        /// <summary>
        /// コンストラクター
        /// </summary>
        public BoardFullColorLED()
        {
            // 各 LED の InputPort インスタンス
            portRed = new OutputPort(pinRed, false);
            portGreen = new OutputPort(pinGreen, false);
            portBlue = new OutputPort(pinBlue, false);
        }

        /// <summary>
        /// 指定の色で LED を点灯、消灯する
        /// </summary>
        /// <param name="redOn">true ならば赤を点灯</param>
        /// <param name="greenOn">true ならば緑を点灯</param>
        /// <param name="blueOn">true ならば青を点灯</param>
        public void SetRgb(bool redOn, bool greenOn, bool blueOn)
        {
            portRed.Write(redOn);
            portGreen.Write(greenOn);
            portBlue.Write(blueOn);
        }

        /// <summary>
        /// 色名指定で LED を点灯する
        /// </summary>
        /// <param name="color"></param>
        public void SetColor(Colors color)
        {
            int redFlag = (int)color & (int)Colors.Red;
            int greenFlag = (int)color & (int)Colors.Green;
            int blueFlag = (int)color & (int)Colors.Blue;
            portRed.Write(redFlag != 0);
            portGreen.Write(greenFlag != 0);
            portBlue.Write(blueFlag != 0);
        }

        public enum Colors
        {
            Black = 0,
            Red = 1,
            Green = 2,
            Yellow = 3,
            Blue = 4,
            Magenta = 5,
            Cyan = 6,
            White = 7
        }
    }
}
