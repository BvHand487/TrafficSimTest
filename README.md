# TrafficSimTest


## Run Locally

#### 1. Clone the project:

```bash
> git clone https://github.com/BvHand487/TrafficSimTest.git
```

#### 2. Go to project directory:

```bash
> cd TrafficSimTest
```

#### 3. Create and activate conda environment:

```bash
> conda create -n ENV_NAME python=3.10.12 && conda activate ENV_NAME
```

#### 4. Download python packages:

```bash
> pip install torch~=2.2.1 --index-url https://download.pytorch.org/whl/cu121
> pip install mlagents==1.1.0
```

#### 5. Open ./TrafficSimTest as a project in Unity 6

#### 6. Run
```bash
> mlagents-learn config.yaml --results-dir=results --run-id=RUN_ID
```

#### 7. When prompted - press Play in the Unity Editor

## Running Tests

No tests yet.