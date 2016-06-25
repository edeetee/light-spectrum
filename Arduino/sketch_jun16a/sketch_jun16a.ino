#include "FastLED.h"
#define NUM_LEDS 200
#define NUM_READS 200
#define FPS 60

CRGB leds[NUM_LEDS];
CRGB readLeds[NUM_READS];
uint8_t readVals[NUM_READS];

void setup() { 
  Serial.begin(115200);
  mapRealToRead();
  FastLED.addLeds<WS2811, 6, BRG>(leds, NUM_LEDS);
  FastLED.setTemperature(Tungsten100W);
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

void loop() {
  if(readData()){
    //drawLeds();
    drawLedsFloat();
    //drawStrobe();
    FastLED.show();
  } else {
    //drawLedsIndividuals();
    //drawLedsDebug();
    //drawStrobe();
    clearLeds();
    FastLED.show();
    //FastLED.delay(1000/60);
  }
}

void drawLeds(){
  int calcIndex;
  for(uint8_t i = 0; i < NUM_LEDS; i++){
    ledMap map = ledMaps[i];
    leds[i] = blend(readLeds[map.left], readLeds[map.right], map.amountOfRight);
  }
}

void drawLedsFloat(){
  int calcIndex;
  uint8_t val;
  uint8_t curHue;
  uint8_t milliMod = (uint8_t)(millis()/(20000/255));
  for(uint8_t i = 0; i < NUM_LEDS; i++){
    ledMap map = ledMaps[i];
    val = readVals[i];
    curHue = val/4 + milliMod + i/10;
    leds[i] = CHSV(curHue, 255 + (200-max(200,val))/2, val);
  }
}

void drawStrobe(){
  static bool isWhite = false;
  isWhite = !isWhite;
  for(uint8_t i = 0; i < NUM_LEDS; i++){
    leds[i] = isWhite ? CRGB::White : CRGB::Black;
  }
}

void drawLedsDebug(){
  uint8_t milliMod = (uint8_t)(millis()/100);
  for(uint8_t i = 0; i < NUM_LEDS; i++){
    leds[i] = CHSV(255*i/NUM_LEDS+milliMod, 255, 255);
  }
}

void drawLedsIndividuals(){
  uint8_t iMod;
  for(uint8_t i = 0; i < NUM_LEDS; i++){
    iMod = i % 3;
    leds[i] = CRGB(255 *(iMod == 0), 255 *(iMod == 1), 255 *(iMod == 2));
  }
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
    while(bytesRead < NUM_READS) {
      bytesRead += Serial.readBytes( (byte*)readVals + bytesRead, NUM_READS-bytesRead);
    }
//    while(bytesRead < NUM_READS*3) {
//      bytesRead += Serial.readBytes( ((byte*)readLeds) + bytesRead, (NUM_READS*3)-bytesRead);
//    }
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
  long lastRecieve = millis();
  char b;
  static int i = 0;
  int timeout = 5;
  while(i < 8){
    b = Serial.read();
    if(b == header[i]){
      long lastRecieve = millis();
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
    //if not recieved but first index
    //} else if(i == 0){
      //return false;
    } else if(100 < (millis() - lastRecieve)){
      Serial.write((millis() - lastRecieve));
      i = 0;
      return false;
    }
  }
  i = 0;
  return true;
}
