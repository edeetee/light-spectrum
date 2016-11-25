#include "FastLED.h"
#define NUM_LEDS 100
#define NUM_READS 50
#define HEADERSIZE 4
#define HEADERTYPES 4

CRGB leds[NUM_LEDS];
CRGB readLeds[NUM_LEDS];
uint8_t readVals[NUM_READS];

typedef enum {DEBUG, SPECTRUM, COLOR} mode;
mode curMode;

fract8 reading;

void setup() { 
  Serial.begin(115200);
  mapRealToRead();
  FastLED.addLeds<WS2811, 6, BRG>(leds, NUM_LEDS);
  FastLED.setCorrection(TypicalLEDStrip);
  FastLED.setTemperature(Candle+150);
  curMode = DEBUG;
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
  readData();
  switch(curMode){
    case DEBUG:
      //drawLedsDebug();
      clearLeds();
      break;
    case SPECTRUM:
      drawLedsReflect();
      break;
    case COLOR:
      drawColor();
      break;
  }
  FastLED.show();
}

void drawColor(){
  static CRGB prevColor;
  static CRGB curColor;
  
  if(reading == 255){
    prevColor = curColor;
    curColor = readLeds[0];
  }
  
  for(uint8_t i = 0; i < NUM_LEDS; i++){
    leds[i] = blend(prevColor, curColor, reading);
  }
}

void drawLeds(){
  int calcIndex;
  for(uint8_t i = 0; i < NUM_LEDS; i++){
    ledMap map = ledMaps[i];
    leds[i] = blend(readLeds[map.left], readLeds[map.right], map.amountOfRight);
  }
}

void drawLedsReflect(){
  int calcIndex;
  uint8_t val;
  uint8_t curHue;
  uint8_t milliMod = (uint8_t)(millis()/(30000/255));
  for(uint8_t i = 0; i < NUM_READS; i++){
    val = readVals[i];
    curHue = i*2;
    curHue += milliMod;
    leds[i] = CHSV(curHue, 255, val);
    leds[(NUM_LEDS-1)-i] = leds[i];
  }
}

void drawLedsDebug(){
  uint8_t milliMod = (uint8_t)(millis()/100);
  for(uint8_t i = 0; i < NUM_LEDS; i++){
    leds[i] = CHSV(255*i/NUM_LEDS+milliMod, 255, 255);
  }
}

void clearLeds(){
  static uint8_t ledMod = 0;
  for(uint8_t i = 0; i < NUM_LEDS; i++){
    leds[i] = CRGB::Black;
  }
  ledMod++;
}

void readData() {
  mode oldMode = curMode;
  
  static long lastRead = millis();
  uint8_t headerType = readHeader();
  if(headerType != -1)
    lastRead = millis();
    
  switch(headerType){
    case 0:
      readSpectrum();
      break;
    case 1:
      readSpectrum();
      break;
    case 3:
      readColor();
      break;
    default:
      break;
  }

  if(3000 < millis()-lastRead)
    curMode = DEBUG;

  if(curMode != oldMode){
    Serial.print("MODE CHANGED");
    Serial.print(oldMode);
    Serial.println(curMode);
  }
}

void readColor(){
  int bytesRead = 0;
  while(bytesRead < sizeof(CRGB)) {
    bytesRead += Serial.readBytes((byte*)readLeds, sizeof(CRGB)-bytesRead);
  }
  curMode = COLOR;
}

void readSpectrum(){
  int bytesRead = 0;
  while(bytesRead < NUM_READS) {
    bytesRead += Serial.readBytes( (byte*)readVals + bytesRead, NUM_READS-bytesRead);
  }
  curMode = SPECTRUM;
}

const char headers[HEADERTYPES][HEADERSIZE] = {
  "SPEC",
  "BEAT",
  "LEDS",
  "RGB1"
};

bool waitForResponse(int wait){
  long start = millis();
  while(!Serial.available())
    if(wait < (millis()-start))
      return false;

  return true;
}

int readHeader(){
  char b;
  int curHeader = -1;
  static uint8_t skipped = 0;
  static uint8_t skippedAvg = 1;
  if(skipped < skippedAvg){
    skipped++;
    reading = (fract8)(255*skipped/(skippedAvg+1));
  }
  
  //clear buffer
  while(Serial.available()){Serial.read();}

  //request data
  Serial.println("READY");

  //wait for a response
  if(!waitForResponse(100))
    return -1;
  
  //find current header
  b = Serial.read();
  for(int i = 0; i < HEADERTYPES; i++){
    if(b == headers[i][0]){
      curHeader = i;
      break;
    }
  }

  if(curHeader == -1){
    //Serial.println("COULD NOT FIND HEADER");
    return -1;
  }

  //read rest of header
  for(int i = 1; i < HEADERSIZE; i++){
    //wait for a response
    if(!waitForResponse(100))
      return -1;
    
    b = Serial.read();
    if(b != headers[curHeader][i]){
//      Serial.print("HEADER: ");
//      Serial.print(curHeader);
//      Serial.print(" AT INDEX: ");
//      Serial.print(i);
//      Serial.print(" RECIEVED: ");
//      Serial.println((uint8_t)b);
      return -1;
    }
  }
  
  Serial.print("READ AFTER FRAMES: ");
  Serial.println(skipped);
  skippedAvg = (skippedAvg+skipped)/2;
  skipped = 0;
  reading = 255;
  //read header correctly
  return curHeader;
}
