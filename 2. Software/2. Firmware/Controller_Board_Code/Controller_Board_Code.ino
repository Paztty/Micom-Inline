#include "IO_port.h"
#include <EEPROM.h>
const String Cmd_getResult = "00";
const String Cmd_startTest = "01";
const String Cmd_resultPBA = "02";
const String Cmd_startQR = "03";
const String Cmd_resultQR = "04";
const String Cmd_resultMode = "05";
const int Cmd_startValue = 64;
const String String_getOK = "@010011*76";
const String String_getNG = "@010000*65";

const String Data_sendTest = "@010100*66";
const String Result_ngPBA = "@010200*67";
const String Result_okPBA1 = "@010201*68";
const String Result_okPBA2 = "@010210*77";
const String Result_okPBA = "@010211*78";

const String Data_sendQR = "@010300*68";
const String Data_skipQR = "@010400*69";
const String Data_enaQR = "@010401*70";
const String Result_ngQR = "@010410*79";
const String Result_okQR = "@010411*80";


const String Mode_1Array = "@010501*71";
const String Mode_2Array = "@010511*81";

const byte State_change = 1;
const byte State_reset = 0;
byte State_changeStart = 0;
byte State_changeQR = 0;
byte State_sendStart = 0;
byte State_sendQR = 0;

int Adr_Reset = 0;
int Adr_Array = 1;
int Adr_QR = 2;
int Ena_ResetPlc = 0;
unsigned long time_waitPlcReset;
//byte State_sendQR = 0;


String Data_com;
String Data_receivePBACurrent;
String Data_receiveQRCurrent;
String Data_skipQRCurrent;
String Data_receiveModeCurrent;
String Data_sendCurrent;
String Data_receivePrevious;


void setup() {
  // put your setup code here, to run once:
  Serial.begin(9600);
  pinMode(Press_Down_Sensor,INPUT);
  pinMode(Scan_QR_Start,INPUT);
  pinMode(Manual_Auto,INPUT);
  pinMode(Reset_Plc,INPUT);
  pinMode(Mode,OUTPUT);
  pinMode(Check_QR,OUTPUT);
  pinMode(Result_QR,OUTPUT);
  pinMode(Check_PBA,OUTPUT);
  pinMode(Result_PBA1,OUTPUT);
  pinMode(Result_PBA2,OUTPUT);
  pinMode(Skip_EnaQR,OUTPUT);

  digitalWrite(Mode, LOW);
  digitalWrite(Check_QR, LOW);
  digitalWrite(Result_QR, LOW);
  digitalWrite(Check_PBA, LOW);
  digitalWrite(Result_PBA1, LOW);
  digitalWrite(Result_PBA2, LOW);
  digitalWrite(Skip_EnaQR, LOW);
  if (EEPROM.read(Adr_Reset) == 255) {
    for(int i = 0; i < EEPROM.length(); i++) EEPROM.write(i, 0);
  }
  else {
    Data_receiveModeCurrent = EEPROM.read(Adr_Array) == 0 ? Mode_2Array : Mode_1Array;
    Data_skipQRCurrent = EEPROM.read(Adr_QR) == 1 ? Data_skipQR : Data_enaQR;
  }
}

//Frame: @ ID-1byte CMD-1byte DATA-1byte * CRC 
// CRC = @(Cmd_startValue) + ID-1*10+ID-0 + ......
void serialEvent(){
  Data_com = Serial.readString();
  //Serial.println(Data_com);
  if (Data_com.length() == 10)
  {
    if (compareString(Data_com, String_getOK)) {
      //Serial.println(Data_com);
      Serial.println(String_getOK);
      State_sendStart = 0;
      State_sendQR = 0;




      
    }
    else if (compareString(Data_com, String_getNG)) {
      if (State_sendStart == 1) Serial.println(Data_sendTest);
      if (State_sendQR == 1)    Serial.println(Data_sendQR);
    }
    else if (validateChecksum(Data_com, getChecksum(Data_com)) == 1) {
      if (String(Data_com.substring(3,5)) == Cmd_resultPBA) {
        //Serial.println("ok");
        Data_receivePBACurrent = Data_com;
      }
      else if (String(Data_com.substring(3,5)) == Cmd_resultQR) {
        Data_receiveQRCurrent = Data_com;
        if (compareString(Data_com, Data_skipQR)) {
          EEPROM.write(Adr_QR,1);
          Data_skipQRCurrent = Data_skipQR;
        }
        if (compareString(Data_com, Data_enaQR)) {
          EEPROM.write(Adr_QR,0);
          Data_skipQRCurrent = Data_enaQR;
        }
      }
      else if (String(Data_com.substring(3,5)) == Cmd_resultMode) {
        Data_receiveModeCurrent = Data_com;
        //Serial.println(Data_receiveModeCurrent);
        if (compareString(Data_com, Mode_1Array)) EEPROM.write(Adr_Array,1);
        else                                      EEPROM.write(Adr_Array,0);
      }
      else {}
      Serial.println(String_getOK);
    }
    else {
      Serial.println(String_getNG);
    }
  }
  else Serial.println(String_getNG);
  
}
// Calculate checksum in the string
int getChecksum(String Data_String)
{  
  unsigned int CRC = 0;
  String String_sub = Data_String.substring(1,7);
  if(Data_String[0] == '@' && Data_String[7] == '*') {
    CRC = CRC + Cmd_startValue;
    
    for (int i = 0; i < String_sub.length(); i++) {
       if (isDigit(String_sub[i])) {
         if (i % 2 == 0) {
          CRC = CRC + String(String_sub[i]).toInt()*10;
         }
         else {
          CRC = CRC + String(String_sub[i]).toInt();
         }
       }
       else {
        CRC = 0;
        break;
       }
    }
  }
  else {
    CRC = 0;
  }
  CRC = CRC % 255;
  //Serial.println(CRC);
  //Serial.println(CRC);
  return CRC;
}
int validateChecksum(String Data_String, int CRC)
{
  String String_sub;
  int Result = 0; 
  bool Check_temp;
  if (Data_String.length() == 10){
    Check_temp = Data_String.substring(8,10).equalsIgnoreCase(String(CRC));
  }
  Result = Check_temp == false ? 0 : 1;
  return Result;
}
bool compareString(String string1, String string2) {
  bool result_temp = true;
  if (string1.length() == string2.length()) {
    for (int i = 0; i < string1.length(); i++) {
      if (String(string1[i]) != String(string2[i])) {
        result_temp = false;
      }
    }
  }
  else {
    result_temp = false;
  }
  return result_temp;
}
void loop() {
  // put your main code here, to run repeatedly:
 if (digitalRead(Manual_Auto) == HIGH) {
    if (digitalRead(Press_Down_Sensor) == LOW && State_changeStart == State_reset) {
      if (State_sendQR == State_reset) {
        State_changeStart = State_change;
        State_sendStart = State_change;
        Data_sendCurrent = Data_sendTest;
        Serial.println(Data_sendTest);
      }
    }
    else if (digitalRead(Press_Down_Sensor) == HIGH) {
      State_changeStart = State_reset;
      Data_receivePBACurrent = "";
      //Serial.println("FF232421");
    }
    else {
      
    }
    if (State_changeStart == State_reset) resetPBA();
    if (compareString(Data_receivePBACurrent, Result_ngPBA)) {
      ngPBA();
      //Serial.println("FFS11");
    }
    else if (compareString(Data_receivePBACurrent, Result_okPBA1)) {
      okPBA1();
      //Serial.println("FF3333");
    }
    else if (compareString(Data_receivePBACurrent, Result_okPBA2)) {
      okPBA2();
      //Serial.println("F555");
    }
    else if (compareString(Data_receivePBACurrent, Result_okPBA)) {
      okPBA();
      //Serial.println("FF7");
    }
    else {
      
    }
    if (digitalRead(Scan_QR_Start) == LOW && State_changeQR == State_reset && compareString(Data_skipQRCurrent, Data_enaQR)) {
      if (State_sendStart == 0) {
        State_changeQR = State_change;
        State_sendQR = State_change;
        Data_sendCurrent = Data_sendQR;
        Serial.println(Data_sendQR);
      }
    }
    else if (digitalRead(Scan_QR_Start) == HIGH) {
      State_changeQR = State_reset;
      Data_receiveQRCurrent = "";
    }
    else {
      
    }
    if (State_changeQR == State_reset && compareString(Data_skipQRCurrent, Data_enaQR)) resetQR();
    if (compareString(Data_skipQRCurrent, Data_skipQR)) {
      skipQR();
      //okQR();
      //Serial.println("wwww6666");
    }
    else if (compareString(Data_receiveQRCurrent, Result_ngQR)) {
      ngQR();
      //Serial.println("wwDDFDww");
      //Serial.println("ng");
    }
    else if (compareString(Data_receiveQRCurrent, Result_okQR)) {
      //Serial.println("ok");
      okQR();
      //Serial.println("www66w");
    }
    else {
      
    }
    if(digitalRead(Reset_Plc) == LOW && Ena_ResetPlc == 0) {
      if (millis - time_waitPlcReset > 3500) {
        Ena_ResetPlc = 1;
        if (compareString(Data_receiveModeCurrent, Mode_1Array)) {
          if (digitalRead(Mode) == HIGH) digitalWrite(Mode,LOW);
          if (digitalRead(Mode) == LOW) digitalWrite(Mode,HIGH);       
        }
        else {
          if (digitalRead(Mode) == LOW) digitalWrite(Mode,HIGH);
          if (digitalRead(Mode) == HIGH) digitalWrite(Mode,LOW);  
        }
      }
    }
    else 
    {
      time_waitPlcReset = millis();
    }
    if (compareString(Data_receiveModeCurrent, Mode_1Array)) {
      oneArray();
      //Serial.println(digitalRead(Mode));
    }
    else if (compareString(Data_receiveModeCurrent, Mode_2Array)) {
      twoArray();
      //Serial.println("333333");
    }
    else {
      
    }
 }
 else {
    lowAll();
    State_changeStart = State_reset;
    State_changeQR = State_reset;
 }
  
}
void ngPBA() {
  if (digitalRead(Result_PBA1) == HIGH) digitalWrite(Result_PBA1,LOW);
  if (digitalRead(Result_PBA2) == HIGH) digitalWrite(Result_PBA2,LOW);
  if (digitalRead(Check_PBA) == LOW) digitalWrite(Check_PBA,HIGH);
}
void okPBA1() {
  if (digitalRead(Result_PBA1) == LOW) digitalWrite(Result_PBA1,HIGH);
  if (digitalRead(Result_PBA2) == HIGH) digitalWrite(Result_PBA2,LOW);
  if (digitalRead(Check_PBA) == LOW) digitalWrite(Check_PBA,HIGH);
}
void okPBA2() {
  if (digitalRead(Result_PBA1) == HIGH) digitalWrite(Result_PBA1,LOW);
  if (digitalRead(Result_PBA2) == LOW) digitalWrite(Result_PBA2,HIGH);
  if (digitalRead(Check_PBA) == LOW) digitalWrite(Check_PBA,HIGH);
}
void okPBA() {
  if (digitalRead(Result_PBA1) == LOW) digitalWrite(Result_PBA1,HIGH);
  if (digitalRead(Result_PBA2) == LOW) digitalWrite(Result_PBA2,HIGH);
  if (digitalRead(Check_PBA) == LOW) digitalWrite(Check_PBA,HIGH);
}
void resetPBA() {
  if (digitalRead(Check_PBA) == HIGH) digitalWrite(Check_PBA,LOW);
  if (digitalRead(Result_PBA1) == HIGH) digitalWrite(Result_PBA1,LOW);
  if (digitalRead(Result_PBA2) == HIGH) digitalWrite(Result_PBA2,LOW);
}
void ngQR() {
  if (digitalRead(Result_QR) == HIGH) digitalWrite(Result_QR,LOW);
  if (digitalRead(Check_QR) == LOW) digitalWrite(Check_QR,HIGH);
}
void okQR() {
  if (digitalRead(Result_QR) == LOW) digitalWrite(Result_QR,HIGH);
  if (digitalRead(Check_QR) == LOW) digitalWrite(Check_QR,HIGH);
}
void skipQR() {
  if (digitalRead(Skip_EnaQR) == LOW) digitalWrite(Skip_EnaQR,HIGH);
}
/*
void enaQR() {
  if (digitalRead(Skip_EnaQR) == HIGH) digitalWrite(Skip_EnaQR,LOW);
}*/
void resetQR() {
  if (digitalRead(Check_QR) == HIGH) digitalWrite(Check_QR,LOW);
  if (digitalRead(Result_QR) == HIGH) digitalWrite(Result_QR,LOW);
  if (digitalRead(Skip_EnaQR) == HIGH) digitalWrite(Skip_EnaQR,LOW);
}
void oneArray() {
  if (digitalRead(Mode) == LOW) digitalWrite(Mode,HIGH);
}
void twoArray() {
  if (digitalRead(Mode) == HIGH) digitalWrite(Mode,LOW);
}
void lowAll() {
  if (digitalRead(Mode) == HIGH) digitalWrite(Mode, LOW);
  if (digitalRead(Check_QR) == HIGH) digitalWrite(Check_QR, LOW);
  if (digitalRead(Result_QR) == HIGH) digitalWrite(Result_QR, LOW);
  if (digitalRead(Check_PBA) == HIGH) digitalWrite(Check_PBA, LOW);
  if (digitalRead(Result_PBA1) == HIGH) digitalWrite(Result_PBA1, LOW);
  if (digitalRead(Result_PBA2) == HIGH) digitalWrite(Result_PBA2, LOW);
}
