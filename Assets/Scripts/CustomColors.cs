using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CustomColors {
    public class PlayerColor {
        public string name;
        public Color hat, overalls;
        public PlayerColor(string name, Color32 hat, Color32 overalls) {
            this.name = name;
            this.hat = hat;
            this.overalls = overalls;
        }
    }

    public static PlayerColor[] Colors = new PlayerColor[] {
                            //HAT                      //OVERALLS
        new("Default",      new(0, 0, 0, 0),           new(0, 0, 0, 0)        ), //default
        new("Blue",         new(70, 155, 255, 255),    new(254, 244, 220, 255)), //blue - mario bros gba player 4
        new("Purple",       new(255, 223, 70, 255),    new(74, 14, 127, 255)  ),
        new("Black",        new(74, 14, 127, 255),     new(46, 40, 51, 255)   ),
        new("Pink",         new(255, 110, 170, 255),   new(255, 215, 110, 255)),
        new("Orange",       new(242, 124, 42, 255),    new(99, 212, 80, 255)  ),
        new("Maroon",       new(126, 39, 57, 255),     new(236, 171, 171, 255)),
        new("SMW",          new(253, 61, 112, 255),    new(89, 225, 211, 255) ), //super mario world
        new("Mindnomad",    new(140, 24, 156, 255),    new(186, 186, 186, 255)), //mindnomad
        new("Chibi",        new(255, 255, 0, 255),     new(54, 54, 54, 255)   ), //chibithemariogamer
    };
}

