#include "FastLED.h"
#define NUM_LEDS 100
#define SPLITS 1
#define NUM_READS NUM_LEDS/SPLITS
#define FPS 60

CRGB leds[NUM_LEDS];
CRGB readLeds[NUM_READS];

void setup() { 
  Serial.begin(115200);
  mapRealToRead();
  FastLED.addLeds<WS2811, 6>(leds, NUM_LEDS);
  FastLED.setCorrection(TypicalLEDStrip);
}

typedef struct LEDMap{
  uint8_t left;
  uint8_t right;
  fract8 amountOfRight;
} ledMap;

ledMap ledMaps[NUM_LEDS];
void mapRealToRead(){
  for(uint8_t i = 0; i < NUM_LEDS; i++){
    double pos;
    double frac = modf((double)NUM_READS*i/NUM_LEDS, &pos);
    if(frac < 0.5){
      ledMaps[i].left = (uint8_t)max(pos-1,0);
      ledMaps[i].right = (uint8_t)pos;
      ledMaps[i].amountOfRight = (fract8)(frac*255+255/2);
    } else {
      ledMaps[i].left = (uint8_t)pos;
      ledMaps[i].right = (uint8_t)min(pos+1,NUM_READS-1);
      ledMaps[i].amountOfRight = (fract8)(frac*255-255/2);
    }
  }
}

bool readOnce = false;
void loop() {
  if(readData()){
    readOnce = true;
    drawLeds();
    FastLED.show();
  }

  if(!readOnce) {
    drawLedsDebug();
    //clearLeds();
    FastLED.show();
  }
  //delay(1000/FPS);
}

void drawLeds(){
  int calcIndex;
  for(uint8_t i = 0; i < NUM_LEDS; i++){
    ledMap map = ledMaps[i];
    leds[i] = blend(readLeds[map.left], readLeds[map.right], map.amountOfRight);
  }
}

void drawLedsDebug(){
  static double ledMod = 0;
  for(uint8_t i = 0; i < NUM_LEDS; i++){
    leds[i] = CHSV(255*(i/10+ledMod)/NUM_LEDS, 255, 255);
  }
  ledMod += 0.01;
}

void clearLeds(){
  static uint8_t ledMod = 0;
  for(uint8_t i = 0; i < NUM_LEDS; i++){
    leds[i] = CRGB::Black;
  }
  ledMod++;
}


bool readData() {
  if(readHeader()){
    int bytesRead = 0;
    while(bytesRead < NUM_READS*3) {
      bytesRead += Serial.readBytes( ((byte*)readLeds) + bytesRead, (NUM_READS*3)-bytesRead);
    }
//    CRGB curPixel;
//    for(int i = 0; i < NUM_READS; i++){
//      curPixel = readLeds[i];
//      Serial.print("pixel ");
//      Serial.print(i);
//      Serial.print(' ');
//      Serial.print(curPixel.r);
//      Serial.print(' ');
//      Serial.print(curPixel.g);
//      Serial.print(' ');
//      Serial.println(curPixel.b);
//    }
//    while(true){
//      if(NUM_READS*3 < Serial.available()){
//        Serial.println("available");
//        Serial.readBytes((uint8_t*)readLeds, NUM_READS*3);
//      }
//    }
//    int i = 0;
//    byte b1;
//    byte b2;
//    byte b3;
//    while(i < NUM_READS){
//      b1 = Serial.read();
//      if(b1 != -1){
//        b2 = Serial.read();
//        if(b2 != -1){
//          b3 = Serial.read();
//          if(b3 != -1){
//            Serial.print(b1);
//            Serial.print(' ');
//            Serial.print(b2);
//            Serial.print(' ');
//            Serial.println(b2);
//            readLeds[i] = CRGB((uint8_t)b1,(uint8_t)b2,(uint8_t)b3);
//            i++;
//          }
//        }
//      }
//    }
    return true;
  }
  return false;
}

const char header[9] = "LEDSTRIP";
bool readHeader(){
  char b;
  static int i = 0;
  int timeout = 5;
  while(i < 8){
    b = Serial.read();
    if(b == header[i]){
      i++;
    //if recieved but not right
    } else if (b != -1) {
      Serial.print("HeaderError error: ");
      Serial.println((uint8_t)b);
      while(Serial.available() > 0){ 
        //Serial.println(Serial.read());
        Serial.read();
      }
      Serial.print('\n');
      i = 0;
      timeout--;
      //CHANGED HERE
      if(timeout <= 0)
        return false;
    //if not recieved but first index
    } else if(i == 0){
      return false;
    }
  }
  i = 0;
  return true;
}
