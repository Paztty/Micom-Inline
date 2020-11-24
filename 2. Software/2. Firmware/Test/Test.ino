
String Response = "";
void setup()
{
    Serial.begin(9600);
    pinMode(2, INPUT);
    pinMode(13, OUTPUT);
    Serial.println("@010100*66");
}

void serialEvent(){  //serialEven
    Response = Serial.readString();
    if(Response == "@010211*78")
    {
        digitalWrite(13,LOW);
    delay(10000);
        Serial.println("@010100*66");
        digitalWrite(13,HIGH);
    }
    if(Response == "@010200*67")
    {
        digitalWrite(13,LOW);
    delay(10000);
        Serial.println("@010100*66");
        digitalWrite(13,HIGH);
    }    
    if(Response == "@010201*68")
    {
        digitalWrite(13,LOW);
    delay(10000);
        Serial.println("@010100*66");
        digitalWrite(13,HIGH);
    } 
    if(Response == "@010210*77")
    {
        digitalWrite(13,LOW);
    delay(10000);
        Serial.println("@010100*66");
        digitalWrite(13,HIGH);
    } 
}

void loop()
{

}
