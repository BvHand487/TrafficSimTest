# TrafficSimTest

Traffic simulation with vehicles and traffic lights made in Unity with ML-Agents for reinforcement learning experiments.
<br/>
<br/>

## 🚀 Running localy

#### 1. Clone the project:

```bash
git clone https://github.com/BvHand487/TrafficSimTest.git
```

#### 2. Open ./TrafficSimTest as a project in Unity 6.

#### 3. Press Play in the Unity Editor toolbar.
<br/>

## 🧠 Training

#### 1. Go to the root of the repository.

#### 2. Create and activate conda environment:

```bash
conda create -n ENV_NAME python=3.10.12 && conda activate ENV_NAME
```

#### 3. Download python packages:

```bash
pip install torch~=2.2.1 --index-url https://download.pytorch.org/whl/cu121
pip install mlagents==1.1.0
```

#### 4. Open ./TrafficSimTest as a project in Unity 6

#### 5. Run
```bash
mlagents-learn config.yaml --results-dir=results --run-id=RUN_ID
```

#### 6. When prompted - press Play in the Unity Editor
<br/>

## 🧪 Running Tests

### Option 1 - Unity Editor:

#### 1. Open the project in Unity.
#### 2. Navigate to Window > General > Test Runner.
#### 3. Click on EditMode/PlayMode and run the tests.
#### 4. Check the results in the panel for output.

>💡 Make sure the Unity Test Framework is installed via Package Manager if the Test Runner isn't visible.  <br/>


### Option 2 - Command Line:
  
#### 1. Close any instances of the Unity Editor.
#### 2. Run the following command from the root of the repository (Windows):
##### Command:
```bash
"C:\Program Files\Unity\Hub\Editor\6000.0.41f1\Editor\Unity.exe" ^
  -runTests ^
  -projectPath ./TrafficSimTest ^
  -testPlatform EditMode ^
  -logFile ./tests/test.log ^
  -testResults ../tests/results.xml ^
  -batchmode ^
  -nographics
```
##### One-liner:
```bash
"C:\Program Files\Unity\Hub\Editor\6000.0.41f1\Editor\Unity.exe" -runTests -projectPath ./TrafficSimTest -testPlatform EditMode -logFile ./tests/test.log -testResults ../tests/results.xml -batchmode -nographics
```

> 💡 Make sure that the path to the Unity executable is valid or is added to PATH. On Windows it's usually something like:<br/>
```"C:\Program Files\Unity\Hub\Editor\VERSION\Editor\Unity.exe"```

##### 📌 Flags explained:
* `-projectPath` - Path of the Unity project
* `-testPlatform` - Either `EditMode` or `PlayMode`
* `-testResults` - Output path for the test results in .xml (NUnit-style)
* `-logFile` - Redirects Unity logs to a file

#### 3. Check the results in ./tests/results.xml.
<br/>

## 📄 License
This project is licensed under the MIT License. For more details, please refer to the LICENSE file.
